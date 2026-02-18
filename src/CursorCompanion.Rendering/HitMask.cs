namespace CursorCompanion.Rendering;

public class HitMask
{
    private const byte AlphaThreshold = 10;

    public static bool TestHit(SkiaRenderer renderer, int localX, int localY)
    {
        byte alpha = renderer.GetAlphaAt(localX, localY);
        return alpha > AlphaThreshold;
    }
}
