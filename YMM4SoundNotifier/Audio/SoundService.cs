using System.IO;
using System.Runtime.CompilerServices;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace YMM4SoundNotifier.Audio;

internal sealed class SoundService : IDisposable
{
    private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan CompletionMargin = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(30);

    private readonly Lock gate = new();
    private readonly List<Playback> active = [];
    private bool disposed;

    public void Play(string filePath, double volume)
        => PlayCore(filePath, volume, waitForCompletion: false);

    public void PlayAndWait(string filePath, double volume)
        => PlayCore(filePath, volume, waitForCompletion: true);

    private void PlayCore(string filePath, double volume, bool waitForCompletion)
    {
        if (disposed) return;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;
        if (volume <= 0d) return;

        StopAll();

        var handle = new PlaybackHandle();

        _ = Task.Run(() =>
        {
            try
            {
                StartPlayback(filePath, (float)Math.Clamp(volume, 0d, 1d), handle);
            }
            catch (Exception)
            {
                handle.MarkStarted(TimeSpan.Zero);
                PlayWithSystemFallback(filePath, waitForCompletion);
                handle.MarkCompleted();
            }
        });

        if (!waitForCompletion) return;

        if (!handle.Started.Wait(StartTimeout))
        {
            StopAll();
            return;
        }

        var wait = handle.Duration > TimeSpan.Zero ? handle.Duration + CompletionMargin : MaxWait;
        if (wait > MaxWait) wait = MaxWait;

        handle.Completed.Wait(wait);
        StopAll();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void StartPlayback(string filePath, float volume, PlaybackHandle handle)
    {
        IDisposable reader;
        IWaveProvider waveProvider;
        TimeSpan duration;

        if (YmmAudioSampleProvider.TryCreate(filePath) is { } ymmSource)
        {
            reader = ymmSource;
            duration = ymmSource.Duration;
            waveProvider = new SampleToWaveProvider(new VolumeSampleProvider(ymmSource) { Volume = volume });
        }
        else
        {
            var audioFile = new AudioFileReader(filePath) { Volume = volume };
            reader = audioFile;
            duration = audioFile.TotalTime;
            waveProvider = audioFile;
        }

        var output = new WaveOut();
        var playback = new Playback(reader, output, handle);

        // ReSharper disable once AccessToDisposedClosure
        output.PlaybackStopped += (_, _) => Finish(playback);
        output.Init(waveProvider);

        lock (gate)
        {
            if (disposed)
            {
                playback.Dispose();
                return;
            }

            active.Add(playback);
        }

        output.Play();
        handle.MarkStarted(duration);
    }

    private static void PlayWithSystemFallback(string filePath, bool waitForCompletion)
    {
        if (!string.Equals(Path.GetExtension(filePath), ".wav", StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            using var player = new System.Media.SoundPlayer(filePath);
            if (waitForCompletion) player.PlaySync();
            else player.Play();
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public void StopAll()
    {
        Playback[] targets;

        lock (gate)
        {
            targets = [.. active];
            active.Clear();
        }

        foreach (var playback in targets)
        {
            playback.Dispose();
        }
    }

    private void Finish(Playback playback)
    {
        lock (gate)
        {
            active.Remove(playback);
        }

        playback.Dispose();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        StopAll();
    }

    private sealed class PlaybackHandle
    {
        public ManualResetEventSlim Started { get; } = new(false);

        public ManualResetEventSlim Completed { get; } = new(false);

        public TimeSpan Duration { get; private set; }

        public void MarkStarted(TimeSpan duration)
        {
            Duration = duration;
            Started.Set();
        }

        public void MarkCompleted()
        {
            Started.Set();
            Completed.Set();
        }
    }

    private sealed class Playback(IDisposable reader, IWavePlayer output, PlaybackHandle handle) : IDisposable
    {
        private int disposedFlag;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposedFlag, 1) != 0) return;

            try
            {
                output.Stop();
            }
            catch (Exception)
            {
                // ignored
            }

            try
            {
                output.Dispose();
            }
            catch (Exception)
            {
                // ignored
            }

            try
            {
                reader.Dispose();
            }
            catch (Exception)
            {
                // ignored
            }

            handle.MarkCompleted();
        }
    }
}
