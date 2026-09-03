using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using YMM4SoundNotifier.Internals;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Resources.Localization;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.ViewModels;

namespace YMM4SoundNotifier.Triggers;

internal sealed class ProjectSaveWatcher : IDisposable
{
    private static readonly TimeSpan SaveFreshness = TimeSpan.FromSeconds(30);

    private readonly MainViewAccessor accessor;
    private readonly Action onSaved;
    private readonly Action onFailed;
    private readonly MessageBoxViewModel? messageBox;
    private readonly INotifyPropertyChanged? model;
    private readonly ICommand?[] saveCommands;
    private readonly ICommand?[] resetCommands;

    private bool isSaveRequested;
    private bool disposed;

    public ProjectSaveWatcher(MainViewAccessor accessor, Action onSaved, Action onFailed)
    {
        this.accessor = accessor;
        this.onSaved = onSaved;
        this.onFailed = onFailed;

        saveCommands = [GetCommand(CommandType.SaveProject), GetCommand(CommandType.SaveProjectAs)];
        resetCommands = [GetCommand(CommandType.OpenProject), GetCommand(CommandType.CreateProject)];

        CommandManager.AddPreviewExecutedHandler(this.accessor.Window, OnPreviewExecuted);

        messageBox = this.accessor.MessageBox;
        if (messageBox is not null) messageBox.Requested += OnMessageBoxRequested;

        model = this.accessor.Model;
        if (model is null) return;

        model.PropertyChanged += OnModelPropertyChanged;
    }

    [SuppressMessage("Performance", "CA1859:可能な場合は具象型を使用してパフォーマンスを向上させる")]
    private static ICommand? GetCommand(CommandType type)
    {
        try
        {
            return SettingsBase<CommandSettings>.Default[type];
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void OnPreviewExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (saveCommands.Contains(e.Command)) isSaveRequested = true;
        else if (resetCommands.Contains(e.Command)) isSaveRequested = false;
    }

    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (disposed) return;
        if (e.PropertyName != "IsProjectFileSaved") return;
        if (!isSaveRequested) return;
        if (Reflect.Property<bool>(model, "IsProjectFileSaved") is not true) return;

        isSaveRequested = false;

        accessor.Window.Dispatcher.BeginInvoke(DispatcherPriority.Background, VerifyAndNotify);
    }

    private void VerifyAndNotify()
    {
        if (disposed) return;

        var path = Reflect.Property<string>(model, "ProjectFilePath");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
        if (DateTime.Now - File.GetLastWriteTime(path) > SaveFreshness) return;

        onSaved();
    }

    private void OnMessageBoxRequested(object? sender, MessageBoxViewModel.MessageBoxEventArgs e)
    {
        if (disposed) return;
        if (e.Caption != Texts.SaveProjectFileFailedDialogTitle) return;

        isSaveRequested = false;
        onFailed();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        CommandManager.RemovePreviewExecutedHandler(accessor.Window, OnPreviewExecuted);

        if (messageBox is not null) messageBox.Requested -= OnMessageBoxRequested;
        if (model is not null) model.PropertyChanged -= OnModelPropertyChanged;
    }
}
