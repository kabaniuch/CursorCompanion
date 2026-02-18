using System.Runtime.InteropServices;
using CursorCompanion.Core;

namespace CursorCompanion.Windowing;

public class LayeredWindow
{
    private IntPtr _hwnd;
    private IntPtr _hdcScreen;
    private IntPtr _hdcMem;
    private IntPtr _hBitmap;
    private IntPtr _pixelData;
    private int _width;
    private int _height;

    public IntPtr Hwnd => _hwnd;
    public int Width => _width;
    public int Height => _height;
    public IntPtr PixelData => _pixelData;

    public void Initialize(IntPtr wpfHwnd, int width, int height)
    {
        _hwnd = wpfHwnd;
        _width = width;
        _height = height;

        // Set extended styles: layered + transparent + toolwindow + topmost + noactivate
        int exStyle = Win32.GetWindowLong(_hwnd, Win32.GWL_EXSTYLE);
        exStyle |= (int)(Win32.WS_EX_LAYERED | Win32.WS_EX_TRANSPARENT |
                         Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_NOACTIVATE);
        Win32.SetWindowLong(_hwnd, Win32.GWL_EXSTYLE, exStyle);

        // Set style to popup
        int style = Win32.GetWindowLong(_hwnd, Win32.GWL_STYLE);
        style = unchecked((int)Win32.WS_POPUP) | (int)Win32.WS_VISIBLE;
        Win32.SetWindowLong(_hwnd, Win32.GWL_STYLE, style);

        // Make topmost
        Win32.SetWindowPos(_hwnd, Win32.HWND_TOPMOST, 0, 0, width, height,
            Win32.SWP_NOMOVE | Win32.SWP_NOACTIVATE | Win32.SWP_SHOWWINDOW);

        // Create DIB section for pixel buffer
        CreateDIBSection();

        Logger.Info($"LayeredWindow initialized: {width}x{height}");
    }

    private void CreateDIBSection()
    {
        _hdcScreen = Win32.CreateCompatibleDC(IntPtr.Zero);
        _hdcMem = Win32.CreateCompatibleDC(_hdcScreen);

        var bmi = new Win32.BITMAPINFO
        {
            bmiHeader = new Win32.BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<Win32.BITMAPINFOHEADER>(),
                biWidth = _width,
                biHeight = -_height, // top-down
                biPlanes = 1,
                biBitCount = 32,
                biCompression = 0 // BI_RGB
            }
        };

        _hBitmap = Win32.CreateDIBSection(_hdcMem, ref bmi, 0, out _pixelData, IntPtr.Zero, 0);
        Win32.SelectObject(_hdcMem, _hBitmap);
    }

    public void Update(int screenX, int screenY)
    {
        var ptDst = new Win32.POINT { X = screenX, Y = screenY };
        var size = new Win32.SIZE { cx = _width, cy = _height };
        var ptSrc = new Win32.POINT { X = 0, Y = 0 };
        var blend = new Win32.BLENDFUNCTION
        {
            BlendOp = Win32.AC_SRC_OVER,
            BlendFlags = 0,
            SourceConstantAlpha = 255,
            AlphaFormat = Win32.AC_SRC_ALPHA
        };

        Win32.UpdateLayeredWindow(_hwnd, IntPtr.Zero, ref ptDst, ref size,
            _hdcMem, ref ptSrc, 0, ref blend, Win32.ULW_ALPHA);
    }

    public void SetClickThrough(bool transparent)
    {
        int exStyle = Win32.GetWindowLong(_hwnd, Win32.GWL_EXSTYLE);
        if (transparent)
            exStyle |= (int)Win32.WS_EX_TRANSPARENT;
        else
            exStyle &= ~(int)Win32.WS_EX_TRANSPARENT;
        Win32.SetWindowLong(_hwnd, Win32.GWL_EXSTYLE, exStyle);
    }

    public void EnsureTopmost()
    {
        Win32.SetWindowPos(_hwnd, Win32.HWND_TOPMOST, 0, 0, 0, 0,
            Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE);
    }

    public void Dispose()
    {
        if (_hBitmap != IntPtr.Zero) Win32.DeleteObject(_hBitmap);
        if (_hdcMem != IntPtr.Zero) Win32.DeleteDC(_hdcMem);
        if (_hdcScreen != IntPtr.Zero) Win32.DeleteDC(_hdcScreen);
    }
}
