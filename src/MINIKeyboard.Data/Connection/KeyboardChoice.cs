using MINIKeyboard.Domain;

namespace MINIKeyboard.Data.Connection;

/// <summary>
/// Which of the keyboards plugged in the app opens — the rule, apart from the HID API so tests can hold it. The one
/// chosen this session, while it stays plugged in; else the one chosen last time (its USB ID and port), when it is
/// plugged in; else the first, in the order the app looks at keyboards, that can be opened at all.
/// </summary>
internal static class KeyboardChoice
{
    /// <summary>What "the one chosen last time" is kept as — its USB ID and port, e.g. <c>1189:8890@USB\VID_1189…</c>.</summary>
    public static string Key(AttachedKeyboard keyboard) => $"{keyboard.Usb}@{keyboard.Location}";

    /// <summary>
    /// The keyboard to open. One the user chose (now or last time) is opened even when it can't be, so the failure is
    /// shown rather than passed over for another.
    /// </summary>
    public static AttachedKeyboard? Target(
        IReadOnlyList<AttachedKeyboard> keyboards, string? chosen, string? remembered,
        Func<AttachedKeyboard, bool> canOpen) =>
        keyboards.FirstOrDefault(keyboard => keyboard.Id == chosen)
        ?? keyboards.FirstOrDefault(keyboard => remembered is not null && Key(keyboard) == remembered)
        ?? keyboards.FirstOrDefault(canOpen)
        ?? keyboards.FirstOrDefault();

    /// <summary>
    /// One entry for each keyboard, in the order its first place to write came — the 1189:8890 first, then the 884x
    /// keyboards in the order the cross-platform app looks for them; several with the same USB ID by port.
    /// </summary>
    public static IReadOnlyList<AttachedKeyboard> Ordered(IEnumerable<AttachedKeyboard> found)
    {
        var unique = found.DistinctBy(keyboard => keyboard.Id, StringComparer.Ordinal).ToList();
        var firstOfUsb = new Dictionary<UsbIdentity, int>();
        for (var index = 0; index < unique.Count; index++)
            firstOfUsb.TryAdd(unique[index].Usb, index);
        return
        [
            .. unique
                .OrderBy(keyboard => firstOfUsb[keyboard.Usb])
                .ThenBy(keyboard => keyboard.Location, StringComparer.Ordinal),
        ];
    }
}
