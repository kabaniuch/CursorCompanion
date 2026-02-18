using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace CursorCompanion.App;

public class SoundService : IDisposable
{
    // C4, E4, G4, C5, E5
    private static readonly float[] Frequencies = { 262f, 330f, 392f, 523f, 659f };
    private const float ToneDuration = 0.3f;

    public void Play(int actionIndex, float volume)
    {
        if (actionIndex < 0 || actionIndex >= Frequencies.Length)
            return;

        var freq = Frequencies[actionIndex];

        var signal = new SignalGenerator()
        {
            Gain = Math.Clamp(volume, 0f, 1f) * 0.3,
            Frequency = freq,
            Type = SignalGeneratorType.Sin
        };

        var take = signal.Take(TimeSpan.FromSeconds(ToneDuration));
        var waveOut = new WaveOutEvent();
        waveOut.Init(take);
        waveOut.PlaybackStopped += (s, e) =>
        {
            waveOut.Dispose();
        };
        waveOut.Play();
    }

    public void Play(string? filePath, float volume, int fallbackIndex)
    {
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            try
            {
                var reader = new AudioFileReader(filePath)
                {
                    Volume = Math.Clamp(volume, 0f, 1f)
                };
                var waveOut = new WaveOutEvent();
                waveOut.Init(reader);
                waveOut.PlaybackStopped += (s, e) =>
                {
                    reader.Dispose();
                    waveOut.Dispose();
                };
                waveOut.Play();
                return;
            }
            catch
            {
                // Fall through to tone
            }
        }

        Play(fallbackIndex, volume);
    }

    public void Dispose()
    {
        // Nothing persistent to dispose — each Play creates and self-disposes
    }
}
