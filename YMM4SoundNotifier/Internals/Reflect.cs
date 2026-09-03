using System.Reflection;

namespace YMM4SoundNotifier.Internals;

internal static class Reflect
{
    private const BindingFlags Instance =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    public static object? Property(object? target, string name)
    {
        if (target is null) return null;

        try
        {
            return target.GetType().GetProperty(name, Instance)?.GetValue(target);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static object? Field(object? target, string name)
    {
        if (target is null) return null;

        try
        {
            return target.GetType().GetField(name, Instance)?.GetValue(target);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static T? Property<T>(object? target, string name)
        => Property(target, name) is T value ? value : default;

    public static T? ReactiveValue<T>(object? owner, string name)
        => Property(Property(owner, name), "Value") is T value ? value : default;
}
