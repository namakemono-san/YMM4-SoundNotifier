using System.IO;
using YukkuriMovieMaker.Commons;

namespace YMM4SoundNotifier.Settings;

public class TriggerSound : Bindable
{
    public bool IsEnabled
    {
        get;
        set => Set(ref field, value);
    }

    public string FilePath
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    public double Volume
    {
        get;
        set => Set(ref field, Math.Clamp(value, 0d, 100d));
    } = 100d;

    public bool CanPlay()
        => IsEnabled && !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath);
}
