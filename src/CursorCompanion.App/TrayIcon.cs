using System.Drawing;
using System.Windows.Forms;
using CursorCompanion.Actions;
using CursorCompanion.Core;

namespace CursorCompanion.App;

public class TrayIcon : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private readonly ActionPackService _actionPackService;
    private readonly Action _onExit;
    private readonly Action _onHost;
    private readonly Action<string> _onConnect;
    private readonly Action _onDisconnect;

    private bool _isHosting;
    private bool _isConnected;

    public TrayIcon(ActionPackService actionPackService, Action onExit,
        Action onHost, Action<string> onConnect, Action onDisconnect)
    {
        _actionPackService = actionPackService;
        _onExit = onExit;
        _onHost = onHost;
        _onConnect = onConnect;
        _onDisconnect = onDisconnect;
    }

    public void SetNetworkState(bool hosting, bool connected)
    {
        _isHosting = hosting;
        _isConnected = connected;
        RebuildMenu();
    }

    public void Initialize()
    {
        _notifyIcon = new NotifyIcon
        {
            Text = "Cursor Companion",
            Visible = true
        };

        // Create a simple icon programmatically
        using var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Orange);
            g.FillEllipse(Brushes.White, 2, 2, 12, 12);
        }
        _notifyIcon.Icon = Icon.FromHandle(bmp.GetHicon());

        RebuildMenu();
    }

    private void RebuildMenu()
    {
        var menu = new ContextMenuStrip();

        // Pack selection submenu
        var packsMenu = new ToolStripMenuItem("Action Packs");
        foreach (var pack in _actionPackService.Packs)
        {
            var item = new ToolStripMenuItem(pack.Name)
            {
                Checked = pack.Id == _actionPackService.ActivePack?.Id,
                Tag = pack.Id
            };
            item.Click += (s, e) =>
            {
                var id = (string)((ToolStripMenuItem)s!).Tag!;
                _actionPackService.SetActivePack(id);
                RebuildMenu();
            };
            packsMenu.DropDownItems.Add(item);
        }
        menu.Items.Add(packsMenu);

        menu.Items.Add(new ToolStripSeparator());

        // Multiplayer submenu
        var mpMenu = new ToolStripMenuItem("Multiplayer");

        if (!_isHosting && !_isConnected)
        {
            var hostItem = new ToolStripMenuItem("Host (port 7777)");
            hostItem.Click += (s, e) => _onHost();
            mpMenu.DropDownItems.Add(hostItem);

            var connectItem = new ToolStripMenuItem("Connect...");
            connectItem.Click += (s, e) =>
            {
                var dialog = new ConnectDialog();
                if (dialog.ShowDialog() == true)
                {
                    _onConnect(dialog.IpAddress);
                }
            };
            mpMenu.DropDownItems.Add(connectItem);
        }
        else
        {
            var statusLabel = _isHosting ? "Hosting..." : "Connected";
            var statusItem = new ToolStripMenuItem(statusLabel) { Enabled = false };
            mpMenu.DropDownItems.Add(statusItem);

            var disconnectItem = new ToolStripMenuItem("Disconnect");
            disconnectItem.Click += (s, e) => _onDisconnect();
            mpMenu.DropDownItems.Add(disconnectItem);
        }

        menu.Items.Add(mpMenu);

        menu.Items.Add(new ToolStripSeparator());

        // Exit
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => _onExit();
        menu.Items.Add(exitItem);

        if (_notifyIcon != null)
            _notifyIcon.ContextMenuStrip = menu;
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}
