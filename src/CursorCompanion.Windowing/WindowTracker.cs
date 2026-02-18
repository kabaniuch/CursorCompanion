using System.Runtime.InteropServices;

namespace CursorCompanion.Windowing;

public struct WindowRect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public int Width => Right - Left;
    public int Height => Bottom - Top;
}

public class WindowTracker
{
    private List<WindowRect> _windowRects = new();
    private readonly object _lock = new();
    private IntPtr _ownHwnd;
    private int _frameCounter;
    private const int RefreshInterval = 10; // every 10 frames

    public void Initialize(IntPtr ownHwnd)
    {
        _ownHwnd = ownHwnd;
        Refresh();
    }

    public void Update()
    {
        _frameCounter++;
        if (_frameCounter >= RefreshInterval)
        {
            _frameCounter = 0;
            Refresh();
        }
    }

    public List<WindowRect> GetWindowRects()
    {
        lock (_lock)
        {
            return new List<WindowRect>(_windowRects);
        }
    }

    private void Refresh()
    {
        var rects = new List<WindowRect>();
        IntPtr shellWnd = Win32.GetShellWindow();
        IntPtr desktopWnd = Win32.GetDesktopWindow();

        Win32.EnumWindows((hWnd, _) =>
        {
            if (hWnd == _ownHwnd || hWnd == shellWnd || hWnd == desktopWnd)
                return true;

            if (!Win32.IsWindowVisible(hWnd))
                return true;

            // Skip zero-size windows
            if (!Win32.GetWindowRect(hWnd, out var wr))
                return true;

            if (wr.Width <= 0 || wr.Height <= 0)
                return true;

            // Skip tool windows and our own layered windows
            int exStyle = Win32.GetWindowLong(hWnd, Win32.GWL_EXSTYLE);
            if ((exStyle & (int)Win32.WS_EX_TOOLWINDOW) != 0)
                return true;

            // Try DWM extended frame bounds for more accurate rect
            Win32.RECT rect;
            int hr = Win32.DwmGetWindowAttribute(hWnd, Win32.DWMWA_EXTENDED_FRAME_BOUNDS,
                out rect, Marshal.SizeOf<Win32.RECT>());
            if (hr != 0)
                rect = wr;

            if (rect.Width > 0 && rect.Height > 0)
            {
                rects.Add(new WindowRect
                {
                    Left = rect.Left,
                    Top = rect.Top,
                    Right = rect.Right,
                    Bottom = rect.Bottom
                });
            }

            return true;
        }, IntPtr.Zero);

        lock (_lock)
        {
            _windowRects = rects;
        }
    }
}
