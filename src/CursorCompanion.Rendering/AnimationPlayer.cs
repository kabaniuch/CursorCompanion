namespace CursorCompanion.Rendering;

public class AnimationPlayer
{
    private AnimClip? _currentClip;
    private float _frameTimer;
    private int _currentFrame;
    private bool _finished;

    public string? CurrentClipName => _currentClip?.Name;
    public int CurrentFrame => _currentFrame;
    public bool IsFinished => _finished;
    public AnimClip? CurrentClip => _currentClip;

    public event Action<string>? OnEvent;
    public event Action? OnClipFinished;

    public void Play(AnimClip clip)
    {
        if (_currentClip == clip && !_finished)
            return;
        _currentClip = clip;
        _currentFrame = 0;
        _frameTimer = 0;
        _finished = false;
    }

    public void Play(SpriteAtlas atlas, string clipName)
    {
        if (atlas.Clips.TryGetValue(clipName, out var clip))
            Play(clip);
    }

    public void Update(float dt)
    {
        if (_currentClip == null || _finished)
            return;

        float frameDuration = 1f / _currentClip.Fps;
        _frameTimer += dt;

        while (_frameTimer >= frameDuration)
        {
            _frameTimer -= frameDuration;
            _currentFrame++;

            // Check for events
            if (_currentClip.Events != null &&
                _currentClip.Events.TryGetValue(_currentFrame, out var evt))
            {
                OnEvent?.Invoke(evt);
            }

            if (_currentFrame >= _currentClip.Frames.Count)
            {
                if (_currentClip.Loop)
                {
                    _currentFrame = 0;
                }
                else
                {
                    _currentFrame = _currentClip.Frames.Count - 1;
                    _finished = true;
                    OnClipFinished?.Invoke();
                    return;
                }
            }
        }
    }

    public FrameRect? GetCurrentFrameRect()
    {
        if (_currentClip == null || _currentClip.Frames.Count == 0)
            return null;
        return _currentClip.Frames[_currentFrame];
    }
}
