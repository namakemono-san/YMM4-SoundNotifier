using System.Runtime.InteropServices;
using YMM4SoundNotifier.Settings;

namespace YMM4SoundNotifier.Triggers;

internal sealed partial class UserPresence
{
    private DateTime lastForegroundUtc = DateTime.UtcNow;

    public int GetElapsedSeconds(IdleDetection detection)
    {
        var inactiveSeconds = UpdateInactiveSeconds();

        return detection == IdleDetection.WindowInactive ? inactiveSeconds : GetIdleSeconds();
    }

    private int UpdateInactiveSeconds()
    {
        if (!IsForeground()) return (int)(DateTime.UtcNow - lastForegroundUtc).TotalSeconds;
        lastForegroundUtc = DateTime.UtcNow;
        return 0;
    }

    private static int GetIdleSeconds()
    {
        var info = new LastInputInfo { cbSize = (uint)Marshal.SizeOf<LastInputInfo>() };
        if (!GetLastInputInfo(ref info)) return 0;

        var elapsed = unchecked((uint)Environment.TickCount - info.dwTime);
        return (int)(elapsed / 1000);
    }

    private static bool IsForeground()
    {
        var window = GetForegroundWindow();
        if (window == IntPtr.Zero) return false;

        _ = GetWindowThreadProcessId(window, out var processId);
        return processId == Environment.ProcessId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint cbSize;
        public uint dwTime;
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetLastInputInfo(ref LastInputInfo plii);

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}
