using System.Runtime.InteropServices;
using SkiaSharp;
using CursorCompanion.Windowing;

namespace CursorCompanion.Rendering;

public class SkiaRenderer : IDisposable
{
    private SKBitmap? _renderTarget;
    private SKCanvas? _canvas;
    private int _width;
    private int _height;

    public int Width => _width;
    public int Height => _height;
    public SKCanvas? Canvas => _canvas;

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _renderTarget = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        _canvas = new SKCanvas(_renderTarget);
    }

    public void Clear()
    {
        _canvas?.Clear(SKColors.Transparent);
    }

    public void DrawFrame(SpriteAtlas atlas, FrameRect frame, int destX, int destY, bool flipX = false)
    {
        if (_canvas == null || atlas.Bitmap == null) return;

        var srcRect = new SKRect(frame.X, frame.Y, frame.X + frame.W, frame.Y + frame.H);
        var dstRect = new SKRect(destX, destY, destX + frame.W, destY + frame.H);

        if (flipX)
        {
            _canvas.Save();
            _canvas.Scale(-1, 1, destX + frame.W / 2f, destY + frame.H / 2f);
            _canvas.DrawBitmap(atlas.Bitmap, srcRect, dstRect);
            _canvas.Restore();
        }
        else
        {
            _canvas.DrawBitmap(atlas.Bitmap, srcRect, dstRect);
        }
    }

    public void CopyToLayeredWindow(LayeredWindow window)
    {
        if (_renderTarget == null) return;

        var pixels = _renderTarget.GetPixels();
        int byteCount = _width * _height * 4;

        unsafe
        {
            Buffer.MemoryCopy(
                (void*)pixels,
                (void*)window.PixelData,
                byteCount,
                byteCount);
        }
    }

    public byte GetAlphaAt(int x, int y)
    {
        if (_renderTarget == null || x < 0 || y < 0 || x >= _width || y >= _height)
            return 0;
        var color = _renderTarget.GetPixel(x, y);
        return color.Alpha;
    }

    public void Dispose()
    {
        _canvas?.Dispose();
        _renderTarget?.Dispose();
    }
}
