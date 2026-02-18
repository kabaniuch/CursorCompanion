using CursorCompanion.Core;
using CursorCompanion.Rendering;
using CursorCompanion.Windowing;
using SkiaSharp;

namespace CursorCompanion.Pet;

public class PetController
{
    public PetPhysics Physics { get; } = new();
    public PetInput Input { get; } = new();
    public AnimationPlayer AnimPlayer { get; } = new();
    public SpriteAtlas Atlas { get; private set; } = null!;

    private readonly StateMachine<PetState> _stateMachine = new();
    private readonly WindowTracker _windowTracker;
    private readonly TaskbarService _taskbarService;
    private readonly SkiaRenderer _renderer;
    private readonly LayeredWindow _layeredWindow;

    private float _idleTimer;
    private const float SleepTimeout = 90f;
    private bool _facingLeft;
    private int _spriteWidth = 64;
    private int _spriteHeight = 64;

    // Render offset — centers the pet sprite in the 256x256 render surface
    private int _renderOffsetX = 96;
    private int _renderOffsetY = 96;

    // Action menu
    private bool _menuVisible;
    private float _menuTimer;
    private const float MenuAutoHideTime = 5f;
    private const int MenuButtonCount = 5;
    private const int MenuButtonSize = 32;
    private const int MenuButtonSpacing = 40;
    private static readonly SKColor[] MenuButtonColors =
    {
        new(0xFF, 0x66, 0x66), // red
        new(0x66, 0xBB, 0xFF), // blue
        new(0x66, 0xFF, 0x88), // green
        new(0xFF, 0xCC, 0x44), // yellow
        new(0xCC, 0x66, 0xFF), // purple
    };

    public event Action<int>? OnMenuAction; // fired when a menu button is clicked (0-4)

    // System menu (middle-click)
    private bool _sysMenuVisible;
    private float _sysMenuTimer;
    private const float SysMenuAutoHideTime = 5f;
    private const int SysMenuItemWidth = 120;
    private const int SysMenuItemHeight = 28;
    private const int SysMenuItemGap = 4;

    /// <summary>0 = None, 1 = Hosting, 2 = Connected</summary>
    public int NetworkState { get; set; }

    public event Action<string>? OnSysMenuAction;

    public PetController(WindowTracker windowTracker, TaskbarService taskbarService,
        SkiaRenderer renderer, LayeredWindow layeredWindow)
    {
        _windowTracker = windowTracker;
        _taskbarService = taskbarService;
        _renderer = renderer;
        _layeredWindow = layeredWindow;

        // Register states
        _stateMachine.RegisterState(PetState.Idle, new IdleState(this));
        _stateMachine.RegisterState(PetState.Falling, new FallingState(this));
        _stateMachine.RegisterState(PetState.Landing, new LandingState(this));
        _stateMachine.RegisterState(PetState.Dragging, new DraggingState(this));
        _stateMachine.RegisterState(PetState.ActionPlaying, new ActionPlayingState(this));
        _stateMachine.RegisterState(PetState.Sleeping, new SleepingState(this));

        // Wire input events
        Input.OnDragStart += () => SetState(PetState.Dragging);
        Input.OnDragEnd += () =>
        {
            Physics.VelocityY = 0;
            SetState(PetState.Falling);
        };
        Input.OnLeftClick += () =>
        {
            // Hide sys menu if open
            _sysMenuVisible = false;

            if (!_menuVisible)
            {
                _menuVisible = true;
                _menuTimer = 0;
            }
            else
            {
                _menuVisible = false;
            }
        };

        Input.OnMiddleClick += () =>
        {
            // Hide action menu if open
            _menuVisible = false;

            _sysMenuVisible = !_sysMenuVisible;
            _sysMenuTimer = 0;
        };
    }

    public void Initialize(SpriteAtlas atlas, int startX, int startY)
    {
        Atlas = atlas;

        // Determine sprite size from first clip
        if (atlas.Clips.Count > 0)
        {
            var firstClip = atlas.Clips.Values.First();
            if (firstClip.Frames.Count > 0)
            {
                _spriteWidth = firstClip.Frames[0].W;
                _spriteHeight = firstClip.Frames[0].H;
            }
        }

        // Center sprite in the render surface
        _renderOffsetX = (_renderer.Width - _spriteWidth) / 2;
        _renderOffsetY = (_renderer.Height - _spriteHeight) / 2;

        Physics.Initialize(_spriteWidth, _spriteHeight);
        Physics.X = startX;
        Physics.Y = startY;

        SetState(PetState.Falling);
        Logger.Info($"Pet initialized at ({startX}, {startY}), sprite: {_spriteWidth}x{_spriteHeight}, offset: ({_renderOffsetX},{_renderOffsetY})");
    }

    public void SetState(PetState state)
    {
        _stateMachine.SetState(state);
        if (state != PetState.Sleeping && state != PetState.Idle)
            _idleTimer = 0;
        // Hide menus on drag/fall/action
        if (state == PetState.Dragging || state == PetState.Falling || state == PetState.ActionPlaying)
        {
            _menuVisible = false;
            _sysMenuVisible = false;
        }
    }

    public PetState CurrentState => _stateMachine.CurrentState;

    public void PlayAnimation(string clipName)
    {
        AnimPlayer.Play(Atlas, clipName);
    }

    public void PlayAction(string clipName)
    {
        PlayAnimation(clipName);
        SetState(PetState.ActionPlaying);
    }

    public void Update(float dt)
    {
        var windows = _windowTracker.GetWindowRects();
        int floorY = _taskbarService.GetFloorY();

        // Window position accounts for render offset
        float windowX = Physics.X - _renderOffsetX;
        float windowY = Physics.Y - _renderOffsetY;

        // Input: pet screen position is its world position (hit-test callback translates to render-target space)
        Input.Update(Physics.X, Physics.Y, _spriteWidth, _spriteHeight,
            (lx, ly) => HitMask.TestHit(_renderer, lx + _renderOffsetX, ly + _renderOffsetY));

        // Handle drag position
        if (_stateMachine.CurrentState == PetState.Dragging)
        {
            Physics.X = Input.DragTargetX;
            Physics.Y = Input.DragTargetY;
        }

        // Physics
        Physics.Update(dt, windows, floorY);

        // State transitions
        var state = _stateMachine.CurrentState;
        if (state == PetState.Idle || state == PetState.Sleeping)
        {
            if (!Physics.HasSupportBelow(windows, floorY))
            {
                SetState(PetState.Falling);
            }
        }

        if (state == PetState.Falling && Physics.IsGrounded)
        {
            SetState(PetState.Landing);
        }

        // Update state machine
        _stateMachine.Update(dt);

        // Animation
        AnimPlayer.Update(dt);

        // Idle timer → sleep
        if (_stateMachine.CurrentState == PetState.Idle)
        {
            _idleTimer += dt;
            if (_idleTimer >= SleepTimeout)
                SetState(PetState.Sleeping);
        }

        // Face cursor
        var (cx, cy) = Input.GetCursorPos();
        float petCenterX = Physics.X + _spriteWidth / 2f;
        _facingLeft = cx < petCenterX;

        // Menu auto-hide timer
        if (_menuVisible)
        {
            _menuTimer += dt;
            if (_menuTimer >= MenuAutoHideTime)
                _menuVisible = false;
        }

        // Menu button click detection (left-click on a button)
        if (_menuVisible)
        {
            bool leftDown = (Win32.GetAsyncKeyState(Win32.VK_LBUTTON) & 0x8000) != 0;
            // We check hit on button in render-target space
            int rtX = cx - (int)windowX;
            int rtY = cy - (int)windowY;
            if (leftDown)
            {
                int hitButton = GetMenuButtonAt(rtX, rtY);
                if (hitButton >= 0)
                {
                    _menuVisible = false;
                    OnMenuAction?.Invoke(hitButton);
                }
            }
        }

        // Sys menu auto-hide timer
        if (_sysMenuVisible)
        {
            _sysMenuTimer += dt;
            if (_sysMenuTimer >= SysMenuAutoHideTime)
                _sysMenuVisible = false;
        }

        // Sys menu click detection
        if (_sysMenuVisible)
        {
            bool leftDown = (Win32.GetAsyncKeyState(Win32.VK_LBUTTON) & 0x8000) != 0;
            int rtX = cx - (int)windowX;
            int rtY = cy - (int)windowY;
            if (leftDown)
            {
                string? hitItem = GetSysMenuItemAt(rtX, rtY);
                if (hitItem != null)
                {
                    _sysMenuVisible = false;
                    OnSysMenuAction?.Invoke(hitItem);
                }
            }
        }

        // Click-through toggle: opaque if cursor over pet OR over visible menu button
        // Convert cursor to render-target space
        int localRtX = cx - (int)(Physics.X - _renderOffsetX);
        int localRtY = cy - (int)(Physics.Y - _renderOffsetY);
        bool overPet = HitMask.TestHit(_renderer, localRtX, localRtY);
        bool overMenu = _menuVisible && GetMenuButtonAt(localRtX, localRtY) >= 0;
        bool overSysMenu = _sysMenuVisible && GetSysMenuItemAt(localRtX, localRtY) != null;
        _layeredWindow.SetClickThrough(!overPet && !overMenu && !overSysMenu);

    }

    private int GetMenuButtonAt(int rtX, int rtY)
    {
        int totalWidth = MenuButtonCount * MenuButtonSpacing;
        int startX = (_renderer.Width - totalWidth) / 2 + MenuButtonSpacing / 2;
        int btnCenterY = _renderOffsetY - 44 + MenuButtonSize / 2;
        int radius = MenuButtonSize / 2;

        for (int i = 0; i < MenuButtonCount; i++)
        {
            int btnCenterX = startX + i * MenuButtonSpacing;
            int dx = rtX - btnCenterX;
            int dy = rtY - btnCenterY;
            if (dx * dx + dy * dy <= radius * radius)
                return i;
        }
        return -1;
    }

    private string[] GetSysMenuItems()
    {
        if (NetworkState == 0)
            return ["Host", "Connect", "Exit"];
        else
            return ["Disconnect", "Exit"];
    }

    private string? GetSysMenuItemAt(int rtX, int rtY)
    {
        var items = GetSysMenuItems();
        int startY = _renderOffsetY + _spriteHeight + 8;
        int centerX = _renderer.Width / 2;
        int itemX = centerX - SysMenuItemWidth / 2;

        for (int i = 0; i < items.Length; i++)
        {
            int itemY = startY + i * (SysMenuItemHeight + SysMenuItemGap);
            if (rtX >= itemX && rtX <= itemX + SysMenuItemWidth &&
                rtY >= itemY && rtY <= itemY + SysMenuItemHeight)
            {
                return items[i].ToLowerInvariant();
            }
        }
        return null;
    }

    public void Render()
    {
        _renderer.Clear();

        // Draw main pet at render offset
        var frame = AnimPlayer.GetCurrentFrameRect();
        if (frame != null)
        {
            _renderer.DrawFrame(Atlas, frame, _renderOffsetX, _renderOffsetY, _facingLeft);
        }

        // Draw action menu buttons
        if (_menuVisible && _renderer.Canvas != null)
        {
            int totalWidth = MenuButtonCount * MenuButtonSpacing;
            int startX = (_renderer.Width - totalWidth) / 2 + MenuButtonSpacing / 2;
            int btnY = _renderOffsetY - 44;

            for (int i = 0; i < MenuButtonCount; i++)
            {
                int btnCenterX = startX + i * MenuButtonSpacing;
                int btnCenterY = btnY + MenuButtonSize / 2;

                // Filled circle
                using var fillPaint = new SKPaint
                {
                    Color = MenuButtonColors[i],
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill
                };
                _renderer.Canvas.DrawCircle(btnCenterX, btnCenterY, MenuButtonSize / 2f, fillPaint);

                // Border
                using var borderPaint = new SKPaint
                {
                    Color = SKColors.White,
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2
                };
                _renderer.Canvas.DrawCircle(btnCenterX, btnCenterY, MenuButtonSize / 2f, borderPaint);

                // Number label
                using var textPaint = new SKPaint
                {
                    Color = SKColors.White,
                    IsAntialias = true,
                    TextSize = 16,
                    TextAlign = SKTextAlign.Center,
                    IsStroke = false
                };
                _renderer.Canvas.DrawText((i + 1).ToString(), btnCenterX, btnCenterY + 6, textPaint);
            }
        }

        // Draw system menu items
        if (_sysMenuVisible && _renderer.Canvas != null)
        {
            var items = GetSysMenuItems();
            int startY = _renderOffsetY + _spriteHeight + 8;
            int centerX = _renderer.Width / 2;
            int itemX = centerX - SysMenuItemWidth / 2;

            for (int i = 0; i < items.Length; i++)
            {
                int itemY = startY + i * (SysMenuItemHeight + SysMenuItemGap);

                using var bgPaint = new SKPaint
                {
                    Color = new SKColor(0x44, 0x44, 0x44, 0xDD),
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill
                };
                var rect = new SKRect(itemX, itemY, itemX + SysMenuItemWidth, itemY + SysMenuItemHeight);
                _renderer.Canvas.DrawRoundRect(rect, 8, 8, bgPaint);

                using var textPaint = new SKPaint
                {
                    Color = SKColors.White,
                    IsAntialias = true,
                    TextSize = 14,
                    TextAlign = SKTextAlign.Center,
                    IsStroke = false
                };
                float textY = itemY + SysMenuItemHeight / 2f + 5f;
                _renderer.Canvas.DrawText(items[i], centerX, textY, textPaint);
            }
        }

        // Copy to layered window; window position accounts for render offset
        _renderer.CopyToLayeredWindow(_layeredWindow);
        _layeredWindow.Update((int)Physics.X - _renderOffsetX, (int)Physics.Y - _renderOffsetY);
    }

    public void WakeUp()
    {
        if (_stateMachine.CurrentState == PetState.Sleeping)
        {
            _idleTimer = 0;
            SetState(PetState.Idle);
        }
    }

}
