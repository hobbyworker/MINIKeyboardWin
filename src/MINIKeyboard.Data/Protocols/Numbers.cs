using MINIKeyboard.Domain;
using static MINIKeyboard.Domain.KnobPart;

namespace MINIKeyboard.Data.Protocols;

/// <summary>
/// Where each control is stored: the 1189:8890's numbers, and the records of the cross-platform app's layouts on a
/// 884x keyboard.
/// </summary>
public static class Numbers
{
    private static readonly Dictionary<string, Dictionary<ControlId, byte>> Knobs = new(StringComparer.Ordinal)
    {
        ["0add1"] = Map(
            [
                (ControlId.Knob(1, RotateLeft), 16), (ControlId.Knob(1, Press), 17),
                (ControlId.Knob(1, RotateRight), 18), (ControlId.Knob(1, PressRotateLeft), 19),
                (ControlId.Knob(1, PressRotateRight), 21),
            ]),
        ["0add2"] = Map(Turns(1, 16), Turns(2, 19)),
        ["0add3"] = Map(Turns(1, 16), Turns(2, 19), Turns(3, 22)),
        ["3add1"] = Map(Turns(1, 16)),
        ["4add1"] = Map(Turns(1, 16)),
        ["4add2"] = Map(Turns(1, 16)),
        ["4add3"] = Map(Turns(1, 16), Turns(2, 19), Turns(3, 22)),
        ["6add1"] = Map(Turns(1, 16)),
        ["6add2"] = Map(Turns(1, 16), Turns(2, 19)),
        ["9add2"] = Map(Turns(1, 16), Turns(2, 19)),
        ["9add3"] = Map(Turns(1, 16), Turns(2, 19), Turns(3, 22)),
        ["11add3"] = Map(Turns(1, 16), Turns(2, 19), Turns(3, 22)),
        ["12add2"] = Map(Turns(1, 16), Turns(2, 19)),
        ["12add3"] = Map(Turns(1, 16), Turns(2, 19), Turns(3, 22)),
        ["12add4"] = Map(Turns(4, 13), Turns(1, 16), Turns(2, 19), Turns(3, 22)),
        ["15add3"] = Map(Turns(1, 16), Turns(2, 19), Turns(3, 22)),
        ["16add3"] = Map(Turns(1, 17), Turns(2, 20), Turns(3, 23)),
        ["21add1"] = Map(
            [
                (ControlId.Knob(1, RotateRight), 22), (ControlId.Knob(1, Press), 23),
                (ControlId.Knob(1, RotateLeft), 24),
            ]),
    };

    /// <summary>
    /// How the 1189:8890 numbers its controls: keys 1–12, then three actions to a knob from 13 — turn left, press, turn
    /// right. The Windows tool has no number for a third knob, or for turning a knob pressed
    /// (<see cref="KeyboardCapabilities.CanAddress"/> says the same in the domain's words).
    /// </summary>
    public static byte? K8890Number(ControlId control)
    {
        if (control.IsKey)
            return control.Number is >= 1 and <= 12 ? control.Number : null;
        return control.Number is >= 1 and <= 2 && control.Part <= RotateRight
            ? (byte)(13 + (control.Number - 1) * 3 + (byte)control.Part)
            : null;
    }

    /// <summary>
    /// The record a 884x keyboard keeps <paramref name="control"/> of <paramref name="layout"/> in: a key in the record
    /// of its number, a knob action where the cross-platform app puts it. In 0add1, records 19 and 21 turn its one knob
    /// while it is pressed — a second knob would have its press in record 20, as in 0add2.
    /// </summary>
    public static byte? Record(ControlId control, KeyboardLayout layout)
    {
        if (!layout.Has(control))
            return null;
        if (control.IsKey)
            return control.Number;
        return Knobs.TryGetValue(layout.Id, out var records) && records.TryGetValue(control, out var record)
            ? record
            : null;
    }

    /// <summary>The control stored in <paramref name="recordId"/>, or null when the layout has none there.</summary>
    public static ControlId? ControlInRecord(byte recordId, KeyboardLayout layout)
    {
        foreach (var control in layout.Controls)
        {
            if (Record(control, layout) == recordId)
                return control;
        }
        return null;
    }

    /// <summary>A knob's turn left, press and turn right, in records from <paramref name="first"/>.</summary>
    private static (ControlId Control, byte Record)[] Turns(byte knob, byte first) =>
    [
        (ControlId.Knob(knob, RotateLeft), first),
        (ControlId.Knob(knob, Press), (byte)(first + 1)),
        (ControlId.Knob(knob, RotateRight), (byte)(first + 2)),
    ];

    private static Dictionary<ControlId, byte> Map(params (ControlId Control, byte Record)[][] knobs) =>
        knobs.SelectMany(knob => knob).ToDictionary(entry => entry.Control, entry => entry.Record);
}
