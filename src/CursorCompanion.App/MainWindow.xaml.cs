using System.IO;
using System.Windows;
using System.Windows.Interop;
using CursorCompanion.Actions;
using CursorCompanion.Core;
using CursorCompanion.Networking;
using CursorCompanion.Pet;
using CursorCompanion.Rendering;
using CursorCompanion.Windowing;

namespace CursorCompanion.App;

public partial class MainWindow : Window
{
    private AppConfig _config = null!;
    private LayeredWindow _layeredWindow = null!;
    private WindowTracker _windowTracker = null!;
    private TaskbarService _taskbarService = null!;
    private SkiaRenderer _renderer = null!;
    private SpriteAtlas _atlas = null!;
    private PetController _pet = null!;
    private ActionPackService _actionPackService = null!;
    private DirectUdpTransport? _transport;
    private GameLoop _gameLoop = null!;
    private TrayIcon _trayIcon = null!;
    private SoundService _soundService = null!;

    // Remote pet
    private Window? _remoteWpfWindow;
    private LayeredWindow? _remoteLayeredWindow;
    private SkiaRenderer? _remoteRenderer;
    private RemotePetController? _remotePet;

    private int _topmostCounter;
    private const int TopmostRefreshInterval = 120; // every 2 seconds at 60fps

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Initialize logger
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        Logger.Init(baseDir);
        Logger.Info("CursorCompanion starting...");

        // Load config
        _config = AppConfig.Load();

        // Get HWND
        var helper = new WindowInteropHelper(this);
        var hwnd = helper.Handle;

        // Initialize layered window
        _layeredWindow = new LayeredWindow();
        _layeredWindow.Initialize(hwnd, _config.WindowWidth, _config.WindowHeight);

        // Initialize window tracker and taskbar
        _windowTracker = new WindowTracker();
        _windowTracker.Initialize(hwnd);

        _taskbarService = new TaskbarService();
        _taskbarService.Refresh();

        // Initialize renderer
        _renderer = new SkiaRenderer();
        _renderer.Initialize(_config.WindowWidth, _config.WindowHeight);

        // Load sprite atlas
        _atlas = new SpriteAtlas();
        var assetsDir = Path.Combine(baseDir, "assets");
        var atlasPng = Path.Combine(assetsDir, "atlas.png");
        var atlasJson = Path.Combine(assetsDir, "atlas.json");

        if (!string.IsNullOrEmpty(_config.AtlasPath) && File.Exists(_config.AtlasPath))
        {
            _atlas.Load(_config.AtlasPath, _config.AtlasJsonPath);
        }
        else if (File.Exists(atlasPng) && File.Exists(atlasJson))
        {
            _atlas.Load(atlasPng, atlasJson);
        }
        else
        {
            Logger.Warn("No atlas found, generating placeholder");
            _atlas.LoadPlaceholder();
        }

        // Initialize action pack service
        _actionPackService = new ActionPackService();
        var actionsJson = Path.Combine(assetsDir, "actions.json");
        var packsJson = Path.Combine(assetsDir, "packs.json");
        _actionPackService.Load(actionsJson, packsJson);
        _actionPackService.SetGlobalCooldown(_config.GlobalCooldown);

        // Initialize sound service
        _soundService = new SoundService();

        // Initialize pet
        _pet = new PetController(_windowTracker, _taskbarService, _renderer, _layeredWindow);
        int startX = Win32.GetSystemMetrics(Win32.SM_CXSCREEN) / 2 - _config.WindowWidth / 2;
        _pet.Initialize(_atlas, startX, 0);

        // Wire hotkey to action
        _pet.Input.OnHotkeyPressed += index =>
        {
            TriggerAction(index);
        };

        // Wire menu button clicks to action + sound
        _pet.OnMenuAction += index =>
        {
            _soundService.Play(index, _config.Volume);
            TriggerAction(index);
        };

        // Wire system menu actions
        _pet.OnSysMenuAction += action =>
        {
            switch (action)
            {
                case "host":
                    _transport = new DirectUdpTransport();
                    _transport.Host(_config.NetworkPort);
                    WireTransportEvents();
                    _pet.NetworkState = 1;
                    _trayIcon.SetNetworkState(hosting: true, connected: false);
                    Logger.Info("Hosting started (via pet menu)");
                    break;

                case "connect":
                    Dispatcher.Invoke(() =>
                    {
                        var dialog = new ConnectDialog();
                        if (dialog.ShowDialog() == true)
                        {
                            _transport = new DirectUdpTransport();
                            _transport.Connect(dialog.IpAddress, _config.NetworkPort);
                            WireTransportEvents();
                            _pet.NetworkState = 2;
                            _trayIcon.SetNetworkState(hosting: false, connected: true);
                            Logger.Info($"Connecting to {dialog.IpAddress} (via pet menu)");
                        }
                    });
                    break;

                case "disconnect":
                    _transport?.Disconnect();
                    _transport = null;
                    DestroyRemotePet();
                    _pet.NetworkState = 0;
                    _trayIcon.SetNetworkState(hosting: false, connected: false);
                    Logger.Info("Disconnected (via pet menu)");
                    break;

                case "exit":
                    Dispatcher.Invoke(() => Close());
                    break;
            }
        };

        // Initialize tray icon
        _trayIcon = new TrayIcon(
            _actionPackService,
            onExit: () => Dispatcher.Invoke(() => Close()),
            onHost: () =>
            {
                _transport = new DirectUdpTransport();
                _transport.Host(_config.NetworkPort);
                WireTransportEvents();
                _trayIcon.SetNetworkState(hosting: true, connected: false);
                Logger.Info("Hosting started");
            },
            onConnect: ip =>
            {
                _transport = new DirectUdpTransport();
                _transport.Connect(ip, _config.NetworkPort);
                WireTransportEvents();
                _trayIcon.SetNetworkState(hosting: false, connected: true);
                Logger.Info($"Connecting to {ip}");
            },
            onDisconnect: () =>
            {
                _transport?.Disconnect();
                _transport = null;
                DestroyRemotePet();
                _trayIcon.SetNetworkState(hosting: false, connected: false);
                Logger.Info("Disconnected");
            }
        );
        _trayIcon.Initialize();

        // Start game loop
        _gameLoop = new GameLoop(Update, Render);
        _gameLoop.Start();

        Logger.Info("CursorCompanion started successfully");
    }

    private void TriggerAction(int index)
    {
        if (_pet.CurrentState == PetState.Idle || _pet.CurrentState == PetState.Sleeping)
        {
            _pet.WakeUp();
            var action = _actionPackService.TriggerAction(index);
            if (action != null)
            {
                _pet.PlayAction(action.ClipName);

                // Send action ping over network
                if (_transport?.IsConnected == true)
                {
                    var msg = MessageSerializer.Serialize(MessageType.ActionPing,
                        new ActionPingMessage
                        {
                            ActionId = action.Id,
                            TimestampUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        });
                    _transport.Send(msg);
                }
            }
        }
    }

    private void WireTransportEvents()
    {
        if (_transport == null) return;

        ITransport t = _transport;
        t.OnReceive += data =>
        {
            if (data.Length == 0) return;
            var msgType = MessageSerializer.GetType(data);
            switch (msgType)
            {
                case MessageType.ActionPing:
                    var (_, actionPing) = MessageSerializer.Deserialize<ActionPingMessage>(data);
                    if (actionPing != null)
                    {
                        var action = _actionPackService.GetAction(actionPing.ActionId);
                        if (action != null)
                            _remotePet?.PlayAction(action.ClipName);
                    }
                    break;

                case MessageType.PositionUpdate:
                    // Ignored — remote pet positions independently
                    break;
            }
        };

        t.OnPeerConnected += () =>
        {
            Dispatcher.Invoke(() => CreateRemotePet());
            Logger.Info("Remote pet connected");
        };

        t.OnPeerDisconnected += () =>
        {
            Dispatcher.Invoke(() => DestroyRemotePet());
            Logger.Info("Remote pet disconnected");
        };
    }

    private void CreateRemotePet()
    {
        if (_remotePet != null) return;

        // Create a hidden WPF Window just for its HWND
        _remoteWpfWindow = new Window
        {
            Width = 0,
            Height = 0,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Transparent
        };
        _remoteWpfWindow.Show();
        var remoteHwnd = new WindowInteropHelper(_remoteWpfWindow).EnsureHandle();

        _remoteLayeredWindow = new LayeredWindow();
        _remoteLayeredWindow.Initialize(remoteHwnd, _config.WindowWidth, _config.WindowHeight);

        _remoteRenderer = new SkiaRenderer();
        _remoteRenderer.Initialize(_config.WindowWidth, _config.WindowHeight);

        _remotePet = new RemotePetController(_windowTracker, _taskbarService,
            _remoteRenderer, _remoteLayeredWindow, _atlas);

        int startX = Win32.GetSystemMetrics(Win32.SM_CXSCREEN) / 2;
        int startY = Win32.GetSystemMetrics(Win32.SM_CYSCREEN) / 2;
        _remotePet.Initialize(startX, startY);
    }

    private void DestroyRemotePet()
    {
        _remotePet?.Dispose();
        _remotePet = null;
        _remoteRenderer = null;
        _remoteLayeredWindow = null;

        if (_remoteWpfWindow != null)
        {
            _remoteWpfWindow.Close();
            _remoteWpfWindow = null;
        }
    }

    private void Update(float dt)
    {
        _windowTracker.Update();

        // Refresh taskbar occasionally
        _topmostCounter++;
        if (_topmostCounter >= TopmostRefreshInterval)
        {
            _topmostCounter = 0;
            _taskbarService.Refresh();
            _layeredWindow.EnsureTopmost();
            _remotePet?.EnsureTopmost();
        }

        _actionPackService.Update(dt);
        _pet.Update(dt);
        _remotePet?.Update(dt);
        _transport?.PollEvents();
    }

    private void Render()
    {
        _pet.Render();
        _remotePet?.Render();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        Logger.Info("CursorCompanion shutting down...");
        _gameLoop?.Stop();
        _transport?.Disconnect();
        DestroyRemotePet();
        _trayIcon?.Dispose();
        _soundService?.Dispose();
        _layeredWindow?.Dispose();
        _renderer?.Dispose();
        _config?.Save();
    }
}
