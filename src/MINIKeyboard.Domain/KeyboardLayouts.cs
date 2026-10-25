using static MINIKeyboard.Domain.KnobPart;

namespace MINIKeyboard.Domain;

/// <summary>The layouts of the cross-platform app and the controls each has — keys, and knob actions.</summary>
internal static class KeyboardLayouts
{
    private static readonly KnobPart[] Turns = [RotateLeft, Press, RotateRight];

    public static readonly IReadOnlyList<KeyboardLayout> All =
    [
        // One knob with five actions: it also turns while pressed. The app draws those two under the turns, with
        // no press of their own — a second knob would have one, as in 0add2.
        Layout("0add1", 0, (1, [RotateLeft, Press, RotateRight, PressRotateLeft, PressRotateRight])),
        Layout("0add2", 0, (1, Turns), (2, Turns)),
        Layout("0add3", 0, (1, Turns), (2, Turns), (3, Turns)),
        Layout("1add0", 1),
        Layout("2add0", 2),
        Layout("3add0", 3),
        Layout("3add1", 3, (1, Turns)),
        Layout("4Key", 4),
        Layout("4add1", 4, (1, Turns)),
        Layout("4add2", 4, (1, Turns)),
        Layout("4add3", 4, (1, Turns), (2, Turns), (3, Turns)),
        Layout("5Key_Mute", 5),
        Layout("6Key", 6),
        Layout("6add1", 6, (1, Turns)),
        Layout("6add2", 6, (1, Turns), (2, Turns)),
        Layout("9add2", 9, (1, Turns), (2, Turns)),
        Layout("9add3", 9, (1, Turns), (2, Turns), (3, Turns)),
        Layout("11add3", 11, (1, Turns), (2, Turns), (3, Turns)),
        Layout("12add0", 12),
        Layout("12add2", 12, (1, Turns), (2, Turns)),
        Layout("12add3", 12, (1, Turns), (2, Turns), (3, Turns)),
        // The fourth knob comes first: the app keeps it in records 13–15, before the other three.
        Layout("12add4", 12, (4, Turns), (1, Turns), (2, Turns), (3, Turns)),
        Layout("15add3", 15, (1, Turns), (2, Turns), (3, Turns)),
        Layout("16add0", 16),
        Layout("16add3", 16, (1, Turns), (2, Turns), (3, Turns)),
        // The app lists this knob's turns the other way round.
        Layout("21add1", 21, (1, [RotateRight, Press, RotateLeft])),
    ];

    /// <summary>Keys 1…<paramref name="keys"/>, then each knob's actions in the order given.</summary>
    private static KeyboardLayout Layout(string id, byte keys, params (byte Knob, KnobPart[] Parts)[] knobs)
    {
        var controls = Enumerable.Range(1, keys).Select(number => ControlId.Key((byte)number))
            .Concat(knobs.SelectMany(knob => knob.Parts.Select(part => ControlId.Knob(knob.Knob, part))));
        return new KeyboardLayout(id, new EquatableArray<ControlId>(controls));
    }
}
