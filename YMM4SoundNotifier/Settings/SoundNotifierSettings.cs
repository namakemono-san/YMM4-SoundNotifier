using System.Diagnostics.CodeAnalysis;
using YMM4SoundNotifier.Settings.Views;
using YukkuriMovieMaker.Plugin;

namespace YMM4SoundNotifier.Settings;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class SoundNotifierSettings : SettingsBase<SoundNotifierSettings>
{
    public override SettingsCategory Category => SettingsCategory.None;

    public override string Name => "YMM4 Sound Notifier";

    public override bool HasSettingView => true;

    public override object SettingView => new SoundNotifierSettingsView();

    public override void Initialize()
    {
    }

    public bool IsEnabled
    {
        get;
        set => Set(ref field, value);
    } = true;

    public double MasterVolume
    {
        get;
        set => Set(ref field, Math.Clamp(value, 0d, 100d));
    } = 100d;

    public IdleDetection IdleDetection
    {
        get;
        set => Set(ref field, value);
    } = IdleDetection.NoInput;

    public int IdleThresholdSeconds
    {
        get;
        set => Set(ref field, Math.Max(10, value));
    } = 300;

    public bool IsIdleRepeated
    {
        get;
        set => Set(ref field, value);
    }

    public TriggerSound Startup
    {
        get;
        set => Set(ref field, value);
    } = new();

    public TriggerSound Shutdown
    {
        get;
        set => Set(ref field, value);
    } = new();

    public TriggerSound ProjectSaved
    {
        get;
        set => Set(ref field, value);
    } = new();

    public TriggerSound ProjectSaveFailed
    {
        get;
        set => Set(ref field, value);
    } = new();

    public TriggerSound VideoOutputCompleted
    {
        get;
        set => Set(ref field, value);
    } = new();

    public TriggerSound VideoOutputFailed
    {
        get;
        set => Set(ref field, value);
    } = new();

    public TriggerSound Idle
    {
        get;
        set => Set(ref field, value);
    } = new();

    public TriggerSound this[TriggerKind kind] => kind switch
    {
        TriggerKind.Startup => Startup,
        TriggerKind.Shutdown => Shutdown,
        TriggerKind.ProjectSaved => ProjectSaved,
        TriggerKind.ProjectSaveFailed => ProjectSaveFailed,
        TriggerKind.VideoOutputCompleted => VideoOutputCompleted,
        TriggerKind.VideoOutputFailed => VideoOutputFailed,
        TriggerKind.Idle => Idle,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
