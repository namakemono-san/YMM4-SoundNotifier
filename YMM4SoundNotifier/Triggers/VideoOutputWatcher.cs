using System.ComponentModel;
using System.Windows.Threading;
using YMM4SoundNotifier.Internals;
using YukkuriMovieMaker.Resources.Localization;
using YukkuriMovieMaker.ViewModels;

namespace YMM4SoundNotifier.Triggers;

internal sealed class VideoOutputWatcher : IDisposable
{
    private const string ProgressViewModelTypeName = "YukkuriMovieMaker.ViewModels.ProgressViewModel";
    private const string FeedbackViewModelTypeName = "YukkuriMovieMaker.ViewModels.FeedbackViewModel";

    private readonly Action onCompleted;
    private readonly Action onFailed;
    private readonly INotifyPropertyChanged? modalViewModel;
    private readonly MessageBoxViewModel? messageBox;
    private readonly Dispatcher dispatcher;

    private object? outputProgress;
    private bool disposed;

    public VideoOutputWatcher(MainViewAccessor accessor, Action onCompleted, Action onFailed)
    {
        this.onCompleted = onCompleted;
        this.onFailed = onFailed;

        dispatcher = accessor.Window.Dispatcher;
        messageBox = accessor.MessageBox;
        modalViewModel = accessor.ModalViewModel;

        if (modalViewModel is null) return;

        modalViewModel.PropertyChanged += OnModalViewModelChanged;
    }

    private void OnModalViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (disposed) return;
        if (e.PropertyName != "Value") return;

        var current = Reflect.Property(modalViewModel, "Value");

        if (current is null)
        {
            var finished = outputProgress;
            outputProgress = null;

            if (finished is null) return;
            if (IsCancelled(finished)) return;

            dispatcher.BeginInvoke(DispatcherPriority.Background, DecideOutcome);
            return;
        }

        if (current.GetType().FullName != ProgressViewModelTypeName) return;
        if (Reflect.Property<string>(current, "Title") != Texts.OutputProgressWindowTitle) return;

        outputProgress = current;
    }

    private void DecideOutcome()
    {
        if (disposed) return;

        if (IsShowingFailure())
        {
            onFailed();
            return;
        }

        onCompleted();
    }

    private bool IsShowingFailure()
    {
        if (messageBox is { ShowingCount: > 0 }) return true;

        var current = Reflect.Property(modalViewModel, "Value");
        return current?.GetType().FullName == FeedbackViewModelTypeName;
    }

    private static bool IsCancelled(object progressViewModel)
        => Reflect.ReactiveValue<bool>(progressViewModel, "IsCancellationRequested");

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        if (modalViewModel is not null) modalViewModel.PropertyChanged -= OnModalViewModelChanged;
    }
}
