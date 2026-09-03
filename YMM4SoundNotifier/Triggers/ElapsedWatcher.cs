namespace YMM4SoundNotifier.Triggers;

internal sealed class ElapsedWatcher : IDisposable
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);

    private readonly Func<int> elapsedSeconds;
    private readonly Func<int> thresholdSeconds;
    private readonly Func<bool> isRepeated;
    private readonly Action onElapsed;
    private readonly Timer timer;

    private bool hasNotified;
    private int nextThresholdSeconds;
    private bool disposed;

    public ElapsedWatcher(Func<int> elapsedSeconds, Func<int> thresholdSeconds, Func<bool> isRepeated, Action onElapsed)
    {
        this.elapsedSeconds = elapsedSeconds;
        this.thresholdSeconds = thresholdSeconds;
        this.isRepeated = isRepeated;
        this.onElapsed = onElapsed;

        nextThresholdSeconds = this.thresholdSeconds();
        timer = new Timer(_ => Poll(), null, PollingInterval, PollingInterval);
    }

    private void Poll()
    {
        if (disposed) return;

        try
        {
            var threshold = Math.Max(1, thresholdSeconds());
            var elapsed = elapsedSeconds();

            if (elapsed < threshold)
            {
                hasNotified = false;
                nextThresholdSeconds = threshold;
                return;
            }

            if (hasNotified && !isRepeated()) return;
            if (elapsed < nextThresholdSeconds) return;

            hasNotified = true;
            nextThresholdSeconds = elapsed + threshold;
            onElapsed();
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        timer.Dispose();
    }
}
