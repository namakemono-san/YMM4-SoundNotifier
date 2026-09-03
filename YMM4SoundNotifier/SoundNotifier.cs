using System.Windows;
using YMM4SoundNotifier.Audio;
using YMM4SoundNotifier.Internals;
using YMM4SoundNotifier.Settings;
using YMM4SoundNotifier.Triggers;

namespace YMM4SoundNotifier;

internal sealed class SoundNotifier : IDisposable
{
    public static SoundNotifier? Current { get; private set; }

    private readonly SoundService sound = new();
    private readonly UserPresence presence = new();
    private readonly List<IDisposable> watchers = [];

    private bool isStartupNotified;
    private bool disposed;

    public void Start()
    {
        Current = this;

        var app = Application.Current;
        if (app is null) return;

        app.Exit += OnApplicationExit;

        watchers.Add(new ElapsedWatcher(
            () => presence.GetElapsedSeconds(SoundNotifierSettings.Default.IdleDetection),
            () => SoundNotifierSettings.Default.IdleThresholdSeconds,
            () => SoundNotifierSettings.Default.IsIdleRepeated,
            () => Notify(TriggerKind.Idle)));

        MainViewHost.Start(OnMainViewAttached);
    }

    private void OnMainViewAttached(MainViewAccessor accessor)
    {
        IDisposable[] windowWatchers =
        [
            new ProjectSaveWatcher(
                accessor,
                () => Notify(TriggerKind.ProjectSaved),
                () => Notify(TriggerKind.ProjectSaveFailed)),

            new VideoOutputWatcher(
                accessor,
                () => Notify(TriggerKind.VideoOutputCompleted),
                () => Notify(TriggerKind.VideoOutputFailed))
        ];

        watchers.AddRange(windowWatchers);
        accessor.Window.Closed += OnWindowClosed;

        if (isStartupNotified) return;

        isStartupNotified = true;
        Notify(TriggerKind.Startup);
        return;

        void OnWindowClosed(object? sender, EventArgs e)
        {
            accessor.Window.Closed -= OnWindowClosed;

            foreach (var watcher in windowWatchers)
            {
                watchers.Remove(watcher);
                DisposeSafely(watcher);
            }
        }
    }

    private void Notify(TriggerKind kind)
    {
        if (disposed) return;

        var settings = SoundNotifierSettings.Default;
        if (!settings.IsEnabled) return;

        var trigger = settings[kind];
        if (!trigger.CanPlay()) return;

        sound.Play(trigger.FilePath, ToGain(trigger, settings));
    }

    public void PlayPreview(TriggerSound trigger)
    {
        if (disposed) return;
        if (string.IsNullOrWhiteSpace(trigger.FilePath)) return;

        sound.Play(trigger.FilePath, ToGain(trigger, SoundNotifierSettings.Default));
    }

    public void StopPreview() => sound.StopAll();

    private static double ToGain(TriggerSound trigger, SoundNotifierSettings settings)
        => trigger.Volume / 100d * settings.MasterVolume / 100d;

    private void OnApplicationExit(object sender, ExitEventArgs e)
    {
        if (disposed) return;

        var settings = SoundNotifierSettings.Default;
        if (!settings.IsEnabled) return;

        var trigger = settings[TriggerKind.Shutdown];
        if (!trigger.CanPlay()) return;

        sound.PlayAndWait(trigger.FilePath, ToGain(trigger, settings));
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        if (Application.Current is { } app) app.Exit -= OnApplicationExit;

        foreach (var watcher in watchers)
        {
            DisposeSafely(watcher);
        }

        watchers.Clear();
        sound.Dispose();

        if (ReferenceEquals(Current, this)) Current = null;
    }

    private static void DisposeSafely(IDisposable watcher)
    {
        try
        {
            watcher.Dispose();
        }
        catch (Exception)
        {
            // ignored
        }
    }
}
