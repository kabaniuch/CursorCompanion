using System.Runtime.InteropServices;

namespace CursorCompanion.Windowing;

public class TaskbarService
{
    private WindowRect _taskbarRect;

    public WindowRect TaskbarRect => _taskbarRect;

    public void Refresh()
    {
        // Try SHAppBarMessage first
        var abd = new Win32.APPBARDATA
        {
            cbSize = (uint)Marshal.SizeOf<Win32.APPBARDATA>()
        };

        uint result = Win32.SHAppBarMessage(Win32.ABM_GETTASKBARPOS, ref abd);
        if (result != 0)
        {
            _taskbarRect = new WindowRect
            {
                Left = abd.rc.Left,
                Top = abd.rc.Top,
                Right = abd.rc.Right,
                Bottom = abd.rc.Bottom
            };
            return;
        }

        // Fallback: find Shell_TrayWnd
        IntPtr trayWnd = Win32.FindWindow("Shell_TrayWnd", null);
        if (trayWnd != IntPtr.Zero && Win32.GetWindowRect(trayWnd, out var rect))
        {
            _taskbarRect = new WindowRect
            {
                Left = rect.Left,
                Top = rect.Top,
                Right = rect.Right,
                Bottom = rect.Bottom
            };
            return;
        }

        // Final fallback: assume bottom taskbar at screen bottom, 48px tall
        int screenW = Win32.GetSystemMetrics(Win32.SM_CXSCREEN);
        int screenH = Win32.GetSystemMetrics(Win32.SM_CYSCREEN);
        _taskbarRect = new WindowRect
        {
            Left = 0,
            Top = screenH - 48,
            Right = screenW,
            Bottom = screenH
        };
    }

    public int GetFloorY()
    {
        return _taskbarRect.Top;
    }
}
