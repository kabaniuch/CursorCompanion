using CursorCompanion.Windowing;

namespace CursorCompanion.Pet;

public class PetInput
{
    private bool _wasRightDown;
    private bool _wasLeftDown;
    private bool _wasMiddleDown;
    private bool _isDragging;
    private int _dragOffsetX;
    private int _dragOffsetY;

    public bool IsDragging => _isDragging;
    public int DragTargetX { get; private set; }
    public int DragTargetY { get; private set; }

    public event Action? OnDragStart;
    public event Action? OnDragEnd;
    public event Action<int>? OnHotkeyPressed; // 0-4
    public event Action? OnLeftClick;
    public event Action? OnMiddleClick;

    private bool[] _wasKeyDown = new bool[5];

    public void Update(float petScreenX, float petScreenY, int spriteWidth, int spriteHeight,
        Func<int, int, bool> hitTest)
    {
        Win32.GetCursorPos(out var cursor);

        // Check right-click drag
        bool rightDown = (Win32.GetAsyncKeyState(Win32.VK_RBUTTON) & 0x8000) != 0;

        if (rightDown && !_wasRightDown)
        {
            // Right button just pressed - check if cursor is over pet
            int localX = cursor.X - (int)petScreenX;
            int localY = cursor.Y - (int)petScreenY;

            if (localX >= 0 && localX < spriteWidth && localY >= 0 && localY < spriteHeight)
            {
                if (hitTest(localX, localY))
                {
                    _isDragging = true;
                    _dragOffsetX = cursor.X - (int)petScreenX;
                    _dragOffsetY = cursor.Y - (int)petScreenY;
                    OnDragStart?.Invoke();
                }
            }
        }
        else if (!rightDown && _wasRightDown && _isDragging)
        {
            _isDragging = false;
            OnDragEnd?.Invoke();
        }

        if (_isDragging)
        {
            DragTargetX = cursor.X - _dragOffsetX;
            DragTargetY = cursor.Y - _dragOffsetY;
        }

        _wasRightDown = rightDown;

        // Check left-click (edge-triggered)
        bool leftDown = (Win32.GetAsyncKeyState(Win32.VK_LBUTTON) & 0x8000) != 0;
        if (leftDown && !_wasLeftDown)
        {
            int localX = cursor.X - (int)petScreenX;
            int localY = cursor.Y - (int)petScreenY;

            if (localX >= 0 && localX < spriteWidth && localY >= 0 && localY < spriteHeight)
            {
                if (hitTest(localX, localY))
                {
                    OnLeftClick?.Invoke();
                }
            }
        }
        _wasLeftDown = leftDown;

        // Check middle-click (edge-triggered)
        bool middleDown = (Win32.GetAsyncKeyState(Win32.VK_MBUTTON) & 0x8000) != 0;
        if (middleDown && !_wasMiddleDown)
        {
            int localX = cursor.X - (int)petScreenX;
            int localY = cursor.Y - (int)petScreenY;

            if (localX >= 0 && localX < spriteWidth && localY >= 0 && localY < spriteHeight)
            {
                if (hitTest(localX, localY))
                {
                    OnMiddleClick?.Invoke();
                }
            }
        }
        _wasMiddleDown = middleDown;

        // Check hotkeys 1-5
        for (int i = 0; i < 5; i++)
        {
            bool keyDown = (Win32.GetAsyncKeyState(Win32.VK_1 + i) & 0x8000) != 0;
            if (keyDown && !_wasKeyDown[i])
            {
                OnHotkeyPressed?.Invoke(i);
            }
            _wasKeyDown[i] = keyDown;
        }
    }

    public (int x, int y) GetCursorPos()
    {
        Win32.GetCursorPos(out var pt);
        return (pt.X, pt.Y);
    }
}
