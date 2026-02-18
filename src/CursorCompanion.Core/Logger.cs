namespace CursorCompanion.Core;

public static class Logger
{
    private static readonly object Lock = new();
    private static string? _logDir;
    private static string? _logFile;

    public static void Init(string baseDir)
    {
        _logDir = Path.Combine(baseDir, "logs");
        Directory.CreateDirectory(_logDir);
        _logFile = Path.Combine(_logDir, $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
    }

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message) => Write("ERROR", message);
    public static void Error(string message, Exception ex) => Write("ERROR", $"{message}: {ex}");

    private static void Write(string level, string message)
    {
        if (_logFile == null) return;
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}";
        lock (Lock)
        {
            try { File.AppendAllText(_logFile, line + Environment.NewLine); }
            catch { /* swallow logging failures */ }
        }
    }
}
