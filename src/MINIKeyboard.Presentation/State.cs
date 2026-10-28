using MINIKeyboard.Domain;

namespace MINIKeyboard.Presentation;

/// <summary>The pages of actions a key can be given.</summary>
public enum EditorTab
{
    Keys,
    Media,
    Mouse,
    Lighting,
    Delay,
}

public static class EditorTabs
{
    public static IReadOnlyList<EditorTab> All { get; } =
        [EditorTab.Keys, EditorTab.Media, EditorTab.Mouse, EditorTab.Lighting, EditorTab.Delay];
}

/// <summary>A transfer with the keyboard under way.</summary>
public enum Activity
{
    Identifying,
    Reading,
    Saving,
}

/// <summary>A switch to another keyboard the user asked for.</summary>
public abstract record KeyboardSwitch
{
    private KeyboardSwitch() { }

    /// <summary>To another of the keyboards plugged in.</summary>
    public sealed record Keyboard(string Id) : KeyboardSwitch;

    /// <summary>To a simulated keyboard, or with a null <paramref name="Id"/> to the keyboard on USB.</summary>
    public sealed record Simulator(string? Id) : KeyboardSwitch;
}

/// <summary>A message that comes and goes.</summary>
public abstract record Notice
{
    private Notice() { }

    /// <summary>Saved; to a simulated keyboard when <paramref name="Simulated"/> — nothing went to a real one.</summary>
    public sealed record Saved(bool Simulated = false) : Notice;

    public sealed record ReadComplete : Notice;

    public sealed record NothingToSave : Notice;

    public sealed record IdentifyFailed : Notice;

    public sealed record ReadFailed : Notice;

    /// <summary>A write failed; <paramref name="Code"/> is the system's error code.</summary>
    public sealed record WriteFailed(int Code) : Notice;

    public sealed record KeyboardGone : Notice;

    /// <summary>The key holds no more presses; <paramref name="Limit"/> is how many it holds.</summary>
    public sealed record Full(int Limit) : Notice;

    public sealed record SelectControlFirst : Notice;

    public sealed record ModifiersOnlyAlone : Notice;

    /// <summary>Colours are kept for the first keys only; <paramref name="Keys"/> is how many.</summary>
    public sealed record NeedsColorKey(int Keys) : Notice;

    public sealed record Unsupported : Notice;

    public bool IsError => this is not (Saved or ReadComplete or NothingToSave);

    /// <summary>How long it stays.</summary>
    public TimeSpan Lifetime => this switch
    {
        _ when IsError => TimeSpan.FromSeconds(6),
        Saved { Simulated: true } => TimeSpan.FromSeconds(4),
        _ => TimeSpan.FromMilliseconds(2500),
    };
}

/// <summary>How actions read on key caps and in lists: the modifiers, then the key — "Ctrl+Shift+C".</summary>
public static class ActionText
{
    private static readonly (Modifiers Modifier, string Name)[] ModifierNames =
    [
        (Domain.Modifiers.Control, "Ctrl"),
        (Domain.Modifiers.RightControl, "RCtrl"),
        (Domain.Modifiers.Alt, "Alt"),
        (Domain.Modifiers.RightAlt, "RAlt"),
        (Domain.Modifiers.Shift, "Shift"),
        (Domain.Modifiers.RightShift, "RShift"),
        (Domain.Modifiers.Super, "Win"),
        (Domain.Modifiers.RightSuper, "RWin"),
    ];

    /// <summary>"Ctrl+Alt+Shift+Win", each right-hand one after its left-hand one and marked "R".</summary>
    public static string Modifiers(Modifiers modifiers) =>
        string.Join("+", ModifierNames.Where(pair => modifiers.HasFlag(pair.Modifier)).Select(pair => pair.Name));

    /// <summary>"Win+C", or "Ctrl+Shift" for modifiers alone.</summary>
    public static string Stroke(KeyStroke stroke)
    {
        var modifiers = Modifiers(stroke.Modifiers);
        if (stroke.IsModifiersOnly)
            return modifiers;
        return modifiers.Length == 0 ? Key(stroke.Key) : $"{modifiers}+{Key(stroke.Key)}";
    }

    /// <summary>The presses one after another.</summary>
    public static string Keys(KeyMacro macro) => string.Join(" ", macro.Strokes.Select(Stroke));

    /// <summary>A key's label, or its usage in hex when the app has no name for it.</summary>
    public static string Key(byte usage) => KeyUsage.Named(usage) is { } key ? key.Label : $"0x{usage:X2}";
}
