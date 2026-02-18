using System.Text.Json;
using System.Text.Json.Serialization;
using SkiaSharp;
using CursorCompanion.Core;

namespace CursorCompanion.Rendering;

public class FrameRect
{
    [JsonPropertyName("x")]
    public int X { get; set; }
    [JsonPropertyName("y")]
    public int Y { get; set; }
    [JsonPropertyName("w")]
    public int W { get; set; }
    [JsonPropertyName("h")]
    public int H { get; set; }
}

public class AnimClip
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    [JsonPropertyName("frames")]
    public List<FrameRect> Frames { get; set; } = new();
    [JsonPropertyName("fps")]
    public int Fps { get; set; } = 12;
    [JsonPropertyName("loop")]
    public bool Loop { get; set; } = true;
    [JsonPropertyName("pivotX")]
    public int PivotX { get; set; }
    [JsonPropertyName("pivotY")]
    public int PivotY { get; set; }
    [JsonPropertyName("events")]
    public Dictionary<int, string>? Events { get; set; }
}

public class AtlasMetadata
{
    [JsonPropertyName("clips")]
    public List<AnimClip> Clips { get; set; } = new();
}

public class SpriteAtlas
{
    public SKBitmap? Bitmap { get; private set; }
    public Dictionary<string, AnimClip> Clips { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool Load(string pngPath, string jsonPath)
    {
        try
        {
            if (!File.Exists(pngPath) || !File.Exists(jsonPath))
            {
                Logger.Warn($"Atlas files not found: {pngPath}");
                return false;
            }

            Bitmap = SKBitmap.Decode(pngPath);
            if (Bitmap == null)
            {
                Logger.Error("Failed to decode atlas PNG");
                return false;
            }

            var json = File.ReadAllText(jsonPath);
            var meta = JsonSerializer.Deserialize<AtlasMetadata>(json);
            if (meta?.Clips != null)
            {
                foreach (var clip in meta.Clips)
                    Clips[clip.Name] = clip;
            }

            Logger.Info($"Loaded atlas: {Bitmap.Width}x{Bitmap.Height}, {Clips.Count} clips");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to load atlas", ex);
            return false;
        }
    }

    public void LoadPlaceholder(int frameSize = 64)
    {
        // Generate a placeholder atlas with colored rectangles for each clip
        string[] clipNames = { "Idle", "Falling", "Landing", "Dragging", "Sleeping",
                               "Scratch", "Roar", "PawWave", "Shake", "SitPose" };
        SKColor[] colors =
        {
            SKColors.Orange, SKColors.Cyan, SKColors.Lime, SKColors.Yellow, SKColors.Purple,
            SKColors.Red, SKColors.Green, SKColors.Blue, SKColors.Magenta, SKColors.Teal
        };

        int framesPerClip = 4;
        int totalFrames = clipNames.Length * framesPerClip;
        int cols = 10;
        int rows = (totalFrames + cols - 1) / cols;

        int atlasW = cols * frameSize;
        int atlasH = rows * frameSize;

        Bitmap = new SKBitmap(atlasW, atlasH, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(Bitmap);
        canvas.Clear(SKColors.Transparent);

        for (int c = 0; c < clipNames.Length; c++)
        {
            var frames = new List<FrameRect>();
            for (int f = 0; f < framesPerClip; f++)
            {
                int idx = c * framesPerClip + f;
                int col = idx % cols;
                int row = idx / cols;
                int x = col * frameSize;
                int y = row * frameSize;

                // Draw colored rectangle with slight variation per frame
                var color = colors[c];
                byte alpha = (byte)(200 + f * 15);
                var paint = new SKPaint
                {
                    Color = new SKColor(color.Red, color.Green, color.Blue, alpha),
                    Style = SKPaintStyle.Fill
                };
                canvas.DrawRect(x + 4, y + 4, frameSize - 8, frameSize - 8, paint);

                // Draw border
                paint.Style = SKPaintStyle.Stroke;
                paint.Color = SKColors.White;
                paint.StrokeWidth = 2;
                canvas.DrawRect(x + 2, y + 2, frameSize - 4, frameSize - 4, paint);

                // Draw label
                paint.Style = SKPaintStyle.Fill;
                paint.Color = SKColors.White;
                paint.TextSize = 10;
                canvas.DrawText(clipNames[c][..Math.Min(3, clipNames[c].Length)],
                    x + 8, y + frameSize / 2 + 4, paint);

                frames.Add(new FrameRect { X = x, Y = y, W = frameSize, H = frameSize });
            }

            bool isAction = c >= 5;
            Clips[clipNames[c]] = new AnimClip
            {
                Name = clipNames[c],
                Frames = frames,
                Fps = 8,
                Loop = !isAction,
                PivotX = frameSize / 2,
                PivotY = frameSize
            };
        }

        Logger.Info($"Generated placeholder atlas: {atlasW}x{atlasH}, {Clips.Count} clips");
    }
}
