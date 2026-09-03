using System.Buffers;
using NAudio.Wave;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Plugin.FileSource;

namespace YMM4SoundNotifier.Audio;

internal sealed class YmmAudioSampleProvider : ISampleProvider, IDisposable
{
    private const int Channels = 2;

    private readonly IAudioFileSource source;
    private bool disposed;

    private YmmAudioSampleProvider(IAudioFileSource source)
    {
        this.source = source;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.Hz, Channels);
    }

    public WaveFormat WaveFormat { get; }

    public TimeSpan Duration => source.Duration;

    public int Read(Span<float> buffer)
    {
        if (disposed) return 0;

        var temporary = ArrayPool<float>.Shared.Rent(buffer.Length);

        try
        {
            var read = source.Read(temporary, 0, buffer.Length);
            if (read > 0) temporary.AsSpan(0, read).CopyTo(buffer);
            return read;
        }
        finally
        {
            ArrayPool<float>.Shared.Return(temporary);
        }
    }

    public static YmmAudioSampleProvider? TryCreate(string filePath)
    {
        IEnumerable<IAudioFileSourcePlugin> plugins;

        try
        {
            plugins = PluginLoader.AudioFileSourcePlugins;
        }
        catch (Exception)
        {
            return null;
        }

        foreach (var plugin in plugins)
        {
            try
            {
                if (plugin.CreateAudioFileSource(filePath, 0) is not { } source) continue;

                return new YmmAudioSampleProvider(source);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        return null;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        if (source is IDisposable disposable) disposable.Dispose();
    }
}
