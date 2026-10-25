namespace MINIKeyboard.Domain.UseCases;

/// <summary>What came of an edit of the app's copy of the configuration.</summary>
public abstract record EditOutcome
{
    private EditOutcome() { }

    public static EditOutcome Applied { get; } = new AppliedOutcome();

    /// <summary>No key or knob is selected.</summary>
    public static EditOutcome NeedsSelection { get; } = new NeedsSelectionOutcome();

    /// <summary>The keyboard has nothing for this, so nothing changed.</summary>
    public static EditOutcome Unsupported { get; } = new UnsupportedOutcome();

    /// <summary>Modifiers alone make a key a modifier key, so they can't follow or precede other presses.</summary>
    public static EditOutcome ModifiersOnlyAlone { get; } = new ModifiersOnlyAloneOutcome();

    public sealed record AppliedOutcome : EditOutcome;

    public sealed record NeedsSelectionOutcome : EditOutcome;

    public sealed record UnsupportedOutcome : EditOutcome;

    public sealed record ModifiersOnlyAloneOutcome : EditOutcome;

    /// <summary>The key holds no more presses; <paramref name="Limit"/> is how many it holds.</summary>
    public sealed record Full(int Limit) : EditOutcome;

    /// <summary>Colours are kept for the first keys only; <paramref name="Keys"/> is how many.</summary>
    public sealed record NeedsColorKey(int Keys) : EditOutcome;
}

/// <summary>Where an edit applies: the selected control on the shown layer of a keyboard with these capabilities.</summary>
public sealed record EditTarget(KeyboardCapabilities Capabilities, int Layer, ControlId? Control);

/// <summary>
/// Adds a press to the selected key. A key that did something else starts over with it; a key that holds modifiers
/// alone takes them into the press.
/// </summary>
public sealed class AppendStrokeUseCase
{
    public EditOutcome Execute(KeyStroke stroke, KeyboardConfiguration configuration, EditTarget target)
    {
        if (target.Control is not { } control)
            return EditOutcome.NeedsSelection;
        if (!stroke.IsModifiersOnly && !target.Capabilities.Takes(stroke.Key))
            return EditOutcome.Unsupported;
        var macro = configuration.Action(control, target.Layer) is KeyAction.Keys current ? current.Macro : KeyMacro.Empty;
        if (macro.Strokes is [{ IsModifiersOnly: true } only])
        {
            stroke = stroke with { Modifiers = stroke.Modifiers | only.Modifiers, Delay = only.Delay };
            macro = macro with { Strokes = [] };
        }
        macro = macro with { Strokes = macro.Strokes.Add(stroke) };
        if (!macro.IsWellFormed)
            return EditOutcome.ModifiersOnlyAlone;
        if (!target.Capabilities.Fits(macro))
            return new EditOutcome.Full(target.Capabilities.PressLimit);
        configuration.Set(new KeyAction.Keys(macro), control, target.Layer);
        return EditOutcome.Applied;
    }
}

/// <summary>Takes the last press off the selected key; the key is cleared when none is left.</summary>
public sealed class RemoveLastStrokeUseCase
{
    public EditOutcome Execute(KeyboardConfiguration configuration, EditTarget target)
    {
        if (target.Control is not { } control)
            return EditOutcome.NeedsSelection;
        if (configuration.Action(control, target.Layer) is not KeyAction.Keys { Macro: var macro } || macro.Strokes.IsEmpty)
            return EditOutcome.Unsupported;
        configuration.Set(EditingRules.KeysOrNothing(macro with { Strokes = macro.Strokes.RemoveLast() }), control, target.Layer);
        return EditOutcome.Applied;
    }
}

/// <summary>Replaces the selected key's action with a media key or a mouse action.</summary>
public sealed class SetActionUseCase
{
    public EditOutcome Execute(KeyAction action, KeyboardConfiguration configuration, EditTarget target)
    {
        if (target.Control is not { } control)
            return EditOutcome.NeedsSelection;
        switch (action)
        {
            case KeyAction.Media { Key: var key } when !target.Capabilities.MediaKeys.Contains(key):
                return EditOutcome.Unsupported;
            case KeyAction.Keys { Macro: var macro }:
                if (!target.Capabilities.TakesKeysOf(macro))
                    return EditOutcome.Unsupported;
                if (!macro.IsWellFormed)
                    return EditOutcome.ModifiersOnlyAlone;
                if (!target.Capabilities.Fits(macro))
                    return new EditOutcome.Full(target.Capabilities.PressLimit);
                break;
        }
        configuration.Set(action, control, target.Layer);
        return EditOutcome.Applied;
    }
}

/// <summary>Sets the wait before one press of the selected key (standard format).</summary>
public sealed class SetPressDelayUseCase
{
    public EditOutcome Execute(int press, uint milliseconds, KeyboardConfiguration configuration, EditTarget target)
    {
        if (target.Capabilities.Delays is not DelaySupport.PerPress { Range: var range })
            return EditOutcome.Unsupported;
        if (target.Control is not { } control)
            return EditOutcome.NeedsSelection;
        if (configuration.Action(control, target.Layer) is not KeyAction.Keys { Macro: var macro }
            || press >= macro.Strokes.Count)
        {
            return EditOutcome.Unsupported;
        }
        var strokes = macro.Strokes.SetItem(press, macro.Strokes[press] with { Delay = range.Clamp(milliseconds) });
        configuration.Set(new KeyAction.Keys(macro with { Strokes = strokes }), control, target.Layer);
        return EditOutcome.Applied;
    }
}

/// <summary>
/// Sets the one wait the selected key keeps (legacy format). Its presses stay; a key that held a media key or a mouse
/// action has none to keep.
/// </summary>
public sealed class SetKeyDelayUseCase
{
    public EditOutcome Execute(uint milliseconds, KeyboardConfiguration configuration, EditTarget target)
    {
        if (target.Capabilities.Delays is not DelaySupport.PerKey { Range: var range })
            return EditOutcome.Unsupported;
        if (target.Control is not { } control)
            return EditOutcome.NeedsSelection;
        var macro = configuration.Action(control, target.Layer) switch
        {
            KeyAction.Keys current => current.Macro,
            KeyAction.Media or KeyAction.Mouse => null,
            _ => KeyMacro.Empty,
        };
        if (macro is null)
            return EditOutcome.Unsupported;
        configuration.Set(EditingRules.KeysOrNothing(macro with { Delay = range.Clamp(milliseconds) }), control, target.Layer);
        return EditOutcome.Applied;
    }
}

/// <summary>Clears the selected key, or every control of the layout on the shown layer.</summary>
public sealed class ClearUseCase
{
    public EditOutcome Execute(KeyboardConfiguration configuration, EditTarget target)
    {
        if (target.Control is not { } control)
            return EditOutcome.NeedsSelection;
        configuration.Set(KeyAction.Unassigned, control, target.Layer);
        return EditOutcome.Applied;
    }

    public EditOutcome ExecuteAll(KeyboardLayout layout, KeyboardConfiguration configuration, int layer)
    {
        foreach (var control in layout.Controls)
            configuration.Set(KeyAction.Unassigned, control, layer);
        return EditOutcome.Applied;
    }
}

/// <summary>Sets the shown layer's lights.</summary>
public sealed class SetLightingUseCase
{
    /// <summary>The 8890's mode, or the mode of a layer of the other formats — keeping its colours.</summary>
    public EditOutcome SetMode(byte mode, KeyboardConfiguration configuration, EditTarget target)
    {
        var current = configuration.Lighting(target.Layer);
        LayerLighting? lighting = target.Capabilities.Lighting switch
        {
            LightingSupport.Modes { Values: var modes } when modes.Contains(mode) => new LayerLighting.ModeOnly(mode),
            LightingSupport.ColorAndMode { ModeRange: var modes } when modes.Contains(mode) =>
                new LayerLighting.ColorMode(current is LayerLighting.ColorMode { Color: var color } ? color : LightColor.Cyan, mode),
            LightingSupport.PerKeyColor { ModeRange: var modes } when modes.Contains(mode) =>
                new LayerLighting.PerKey(mode, current is LayerLighting.PerKey { Colors: var colors } ? colors : KeyColor.DefaultKeyColors),
            _ => null,
        };
        if (lighting is null)
            return EditOutcome.Unsupported;
        configuration.SetLighting(lighting, target.Layer);
        return EditOutcome.Applied;
    }

    /// <summary>The colour of a layer that takes one (legacy format and unidentified keyboards).</summary>
    public EditOutcome SetColor(LightColor color, KeyboardConfiguration configuration, EditTarget target)
    {
        if (target.Capabilities.Lighting is not LightingSupport.ColorAndMode { ModeRange: var modes })
            return EditOutcome.Unsupported;
        var mode = configuration.Lighting(target.Layer) is LayerLighting.ColorMode current ? current.Mode : modes.First;
        configuration.SetLighting(new LayerLighting.ColorMode(color, mode), target.Layer);
        return EditOutcome.Applied;
    }

    /// <summary>The colour of the selected key (standard format), which must be one of the first keys.</summary>
    public EditOutcome SetKeyColor(KeyColor keyColor, KeyboardConfiguration configuration, EditTarget target)
    {
        if (target.Capabilities.Lighting is not LightingSupport.PerKeyColor { ModeRange: var modes, Keys: var keys })
            return EditOutcome.Unsupported;
        if (target.Control is not { } control)
            return EditOutcome.NeedsSelection;
        if (ColorIndex(control, keys) is not { } index)
            return new EditOutcome.NeedsColorKey(keys);
        var (mode, colors) = configuration.Lighting(target.Layer) is LayerLighting.PerKey current
            ? (current.Mode, current.Colors)
            : (modes.First, KeyColor.DefaultKeyColors);
        configuration.SetLighting(new LayerLighting.PerKey(mode, colors.SetItem(index, keyColor)), target.Layer);
        return EditOutcome.Applied;
    }

    /// <summary>
    /// Where a key's colour is kept: keys 1–16 have colours 1–16. (The app colours records 1–16, and a key is stored in
    /// the record of its number.)
    /// </summary>
    public static int? ColorIndex(ControlId control, int keys) =>
        control.IsKey && control.Number >= 1 && control.Number <= keys ? control.Number - 1 : null;
}

internal static class EditingRules
{
    /// <summary>The key's presses and wait, or nothing when it has neither.</summary>
    public static KeyAction KeysOrNothing(KeyMacro macro) =>
        macro.Strokes.IsEmpty && macro.Delay == 0 ? KeyAction.Unassigned : new KeyAction.Keys(macro);
}
