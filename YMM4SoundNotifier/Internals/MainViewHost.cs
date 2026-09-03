using System.ComponentModel;
using System.Windows;
using YukkuriMovieMaker.ViewModels;

namespace YMM4SoundNotifier.Internals;

internal sealed class MainViewAccessor(Window window)
{
    public Window Window { get; } = window;

    private object? ViewModel => Window.DataContext;

    public INotifyPropertyChanged? Model => Reflect.Field(ViewModel, "model") as INotifyPropertyChanged;

    public INotifyPropertyChanged? ModalViewModel => Reflect.Property(ViewModel, "ModalViewModel") as INotifyPropertyChanged;

    public MessageBoxViewModel? MessageBox => Reflect.Property(ViewModel, "MessageBoxViewModel") as MessageBoxViewModel;
}

internal static class MainViewHost
{
    private const string MainViewTypeName = "YukkuriMovieMaker.Views.MainView";

    private static readonly HashSet<Window> Attached = [];
    private static Action<MainViewAccessor>? _onAttached;
    private static bool _started;

    public static void Start(Action<MainViewAccessor> handler)
    {
        var app = Application.Current;

        app?.Dispatcher.InvokeAsync(() =>
        {
            _onAttached = handler;

            if (!_started)
            {
                _started = true;
                EventManager.RegisterClassHandler(
                    typeof(Window),
                    FrameworkElement.LoadedEvent,
                    new RoutedEventHandler(OnWindowLoaded));
            }

            foreach (var window in app.Windows.OfType<Window>().ToArray())
            {
                TryAttach(window);
            }
        });
    }

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Window window) TryAttach(window);
    }

    private static void TryAttach(Window window)
    {
        if (window.GetType().FullName != MainViewTypeName) return;
        if (!Attached.Add(window)) return;

        window.Closed += OnWindowClosed;

        try
        {
            _onAttached?.Invoke(new MainViewAccessor(window));
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private static void OnWindowClosed(object? sender, EventArgs e)
    {
        if (sender is not Window window) return;

        window.Closed -= OnWindowClosed;
        Attached.Remove(window);
    }
}
