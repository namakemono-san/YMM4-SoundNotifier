using System.Diagnostics.CodeAnalysis;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Plugin.Update;

namespace YMM4SoundNotifier;

[PluginDetails(AuthorName = "namakemono-san", ContentId = "")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
public class Plugin : IPlugin, IDisposable
{
    public string Name => "YMM4 Sound Notifier";

    public IPluginUpdater Updater => new GitHubReleasesPluginUpdater<Plugin>("namakemono-san", "YMM4-SoundNotifier");

    private readonly SoundNotifier notifier = new();

    public Plugin() => notifier.Start();

    public void Dispose()
    {
        notifier.Dispose();
        GC.SuppressFinalize(this);
    }
}
