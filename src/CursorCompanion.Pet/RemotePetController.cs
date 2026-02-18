using CursorCompanion.Rendering;
using CursorCompanion.Windowing;

namespace CursorCompanion.Pet;

public class RemotePetController : IDisposable
{
    private enum State { Idle, Falling, Landing, Dragging, ActionPlaying }

    private readonly WindowTracker _windowTracker;
    private readonly TaskbarService _taskbarService;
    private readonly SkiaRenderer _renderer;
    private readonly LayeredWindow _layeredWindow;
    private readonly SpriteAtlas _atlas;
    private readonly PetPhysics _physics = new();
    private readonly AnimationPlayer _animPlayer = new();

    private int _spriteWidth = 64;
    private int _spriteHeight = 64;
    private int _renderOffsetX = 96;
    private int _renderOffsetY = 96;
    private bool _facingLeft;

    // Drag state
    private bool _wasRightDown;
    private bool _isDragging;
    private int _dragOffsetX;
    private int _dragOffsetY;

    private State _state = State.Falling;
    private float _landTimer;
    private const float LandDuration = 0.3f;

    public RemotePetController(WindowTracker windowTracker, TaskbarService taskbarService,
        SkiaRenderer renderer, LayeredWindow layeredWindow, SpriteAtlas atlas)
    {
        _windowTracker = windowTracker;
        _taskbarService = taskbarService;
        _renderer = renderer;
        _layeredWindow = layeredWindow;
        _atlas = atlas;
    }

    public void Initialize(int startX, int startY)
    {
        // Determine sprite size from atlas
        if (_atlas.Clips.Count > 0)
        {
            var firstClip = _atlas.Clips.Values.First();
            if (firstClip.Frames.Count > 0)
            {
                _spriteWidth = firstClip.Frames[0].W;
                _spriteHeight = firstClip.Frames[0].H;
            }
        }

        _renderOffsetX = (_renderer.Width - _spriteWidth) / 2;
        _renderOffsetY = (_renderer.Height - _spriteHeight) / 2;

        _physics.Initialize(_spriteWidth, _spriteHeight);
        _physics.X = startX;
        _physics.Y = startY;

        _state = State.Falling;
        _animPlayer.Play(_atlas, "Falling");
    }

    public void PlayAction(string clipName)
    {
        if (_atlas.Clips.ContainsKey(clipName))
        {
            _animPlayer.Play(_atlas, clipName);
            _state = State.ActionPlaying;
        }
    }

    public void Update(float dt)
    {
        var windows = _windowTracker.GetWindowRects();
        int floorY = _taskbarService.GetFloorY();

        // --- Right-click drag input ---
        Win32.GetCursorPos(out var cursor);
        bool rightDown = (Win32.GetAsyncKeyState(Win32.VK_RBUTTON) & 0x8000) != 0;

        if (rightDown && !_wasRightDown)
        {
            int localX = cursor.X - (int)_physics.X;
            int localY = cursor.Y - (int)_physics.Y;

            if (localX >= 0 && localX < _spriteWidth && localY >= 0 && localY < _spriteHeight)
            {
                if (HitMask.TestHit(_renderer, localX + _renderOffsetX, localY + _renderOffsetY))
                {
                    _isDragging = true;
                    _dragOffsetX = cursor.X - (int)_physics.X;
                    _dragOffsetY = cursor.Y - (int)_physics.Y;
                    _state = State.Dragging;
                    _physics.Enabled = false;
                    _animPlayer.Play(_atlas, "Dragging");
                }
            }
        }
        else if (!rightDown && _wasRightDown && _isDragging)
        {
            _isDragging = false;
            _physics.Enabled = true;
            _physics.VelocityY = 0;
            _state = State.Falling;
            _animPlayer.Play(_atlas, "Falling");
        }

        if (_isDragging)
        {
            _physics.X = cursor.X - _dragOffsetX;
            _physics.Y = cursor.Y - _dragOffsetY;
        }

        _wasRightDown = rightDown;

        // --- Physics ---
        _physics.Update(dt, windows, floorY);

        // --- State transitions ---
        if (_state == State.Idle)
        {
            if (!_physics.HasSupportBelow(windows, floorY))
            {
                _state = State.Falling;
                _animPlayer.Play(_atlas, "Falling");
            }
        }

        if (_state == State.Falling && _physics.IsGrounded)
        {
            _state = State.Landing;
            _landTimer = 0;
            _animPlayer.Play(_atlas, "Landing");
        }

        if (_state == State.Landing)
        {
            _landTimer += dt;
            if (_landTimer >= LandDuration || _animPlayer.IsFinished)
            {
                _state = State.Idle;
                _animPlayer.Play(_atlas, "Idle");
            }
        }

        if (_state == State.ActionPlaying && _animPlayer.IsFinished)
        {
            _state = State.Idle;
            _animPlayer.Play(_atlas, "Idle");
        }

        // --- Animation ---
        _animPlayer.Update(dt);

        // --- Face cursor ---
        float petCenterX = _physics.X + _spriteWidth / 2f;
        _facingLeft = cursor.X < petCenterX;

        // --- Click-through ---
        int localRtX = cursor.X - (int)(_physics.X - _renderOffsetX);
        int localRtY = cursor.Y - (int)(_physics.Y - _renderOffsetY);
        bool overPet = HitMask.TestHit(_renderer, localRtX, localRtY);
        _layeredWindow.SetClickThrough(!overPet);
    }

    public void Render()
    {
        _renderer.Clear();

        var frame = _animPlayer.GetCurrentFrameRect();
        if (frame != null)
        {
            _renderer.DrawFrame(_atlas, frame, _renderOffsetX, _renderOffsetY, _facingLeft);
        }

        _renderer.CopyToLayeredWindow(_layeredWindow);
        _layeredWindow.Update((int)_physics.X - _renderOffsetX, (int)_physics.Y - _renderOffsetY);
    }

    public void EnsureTopmost()
    {
        _layeredWindow.EnsureTopmost();
    }

    public void Dispose()
    {
        _renderer.Dispose();
        _layeredWindow.Dispose();
    }
}
