namespace CursorCompanion.Core;

public static class Time
{
    public static float DeltaTime { get; internal set; }
    public static float TotalTime { get; internal set; }
    public const float FixedDt = 1f / 60f;
}
