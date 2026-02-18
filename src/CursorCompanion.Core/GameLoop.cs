using System.Diagnostics;

namespace CursorCompanion.Core;

public class GameLoop
{
    private readonly Action<float> _update;
    private readonly Action _render;
    private volatile bool _running;
    private Thread? _thread;

    private const double TargetFrameTime = 1000.0 / 60.0; // ~16.66ms

    public GameLoop(Action<float> update, Action render)
    {
        _update = update;
        _render = render;
    }

    public void Start()
    {
        if (_running) return;
        _running = true;
        _thread = new Thread(Loop)
        {
            IsBackground = true,
            Name = "GameLoop",
            Priority = ThreadPriority.AboveNormal
        };
        _thread.Start();
    }

    public void Stop()
    {
        _running = false;
        _thread?.Join(500);
    }

    private void Loop()
    {
        var stopwatch = Stopwatch.StartNew();
        double accumulated = 0;
        double lastTime = 0;

        while (_running)
        {
            double currentTime = stopwatch.Elapsed.TotalMilliseconds;
            double elapsed = currentTime - lastTime;
            lastTime = currentTime;

            accumulated += elapsed;

            // Cap accumulation to prevent spiral of death
            if (accumulated > TargetFrameTime * 5)
                accumulated = TargetFrameTime * 5;

            while (accumulated >= TargetFrameTime)
            {
                Time.DeltaTime = Time.FixedDt;
                Time.TotalTime += Time.FixedDt;
                _update(Time.FixedDt);
                accumulated -= TargetFrameTime;
            }

            _render();

            // Sleep for remaining time
            double remaining = TargetFrameTime - (stopwatch.Elapsed.TotalMilliseconds - lastTime);
            if (remaining > 1)
                Thread.Sleep((int)remaining);
        }
    }
}
