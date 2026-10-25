using MINIKeyboard.Domain;

namespace MINIKeyboard.Presentation;

// What each part of the window draws, in the presentation's types.
public sealed partial class EditorModel
{
    // Header

    public ConnectionStatus Status
    {
        get
        {
            switch (state.Connection)
            {
                case ConnectionState.Connected { Device: var keyboard }:
                    var identity = state.Identification?.Identity;
                    return new ConnectionStatus.Connected(
                        keyboard.Usb.ToString(),
                        identity?.KeyCount,
                        identity?.KnobCount,
                        state.Identification is not null,
                        state.KeyboardSource.IsSimulated);
                case ConnectionState.NeedsPermission:
                    return new ConnectionStatus.NeedsPermission();
                case ConnectionState.OpenFailed failed:
                    return new ConnectionStatus.OpenFailed(failed.Code);
                default:
                    return new ConnectionStatus.Searching();
            }
        }
    }

    public KeyboardInfo? KeyboardInfo =>
        state.Keyboard is { } keyboard ? KeyboardInfo.Of(keyboard, state.Identification) : null;

    // The keyboard

    public KeyboardDrawing? Drawing
    {
        get
        {
            if (state.ResolvedLayout is not { } resolved || LayoutGeometry.Of(resolved.Layout) is not { } upright)
                return null;
            var geometry = upright.Rotated(resolved.Rotation);
            var controls = new List<ControlDrawing>();
            foreach (var control in resolved.Layout.Controls)
            {
                if (!geometry.Rects.TryGetValue(control, out var rect))
                    continue;
                ControlFace face = control.IsKey
                    ? new ControlFace.Key(control.Number)
                    : new ControlFace.Knob(control.Part.ToKnobAction());
                Rgb? ledColor = state.Tab == EditorTab.Lighting && state.KeyColor(control) is { } color
                    ? Rgb.Of(color)
                    : null;
                controls.Add(new ControlDrawing(
                    new ControlRef(control),
                    face,
                    rect,
                    ActionCaption.Of(state.Action(control)),
                    state.Selection == control,
                    state.Configuration.IsChanged(control, state.Layer),
                    ledColor));
            }
            return new KeyboardDrawing(geometry.Frame, geometry.Bounds(), [.. controls]);
        }
    }

    // Selection

    public ControlRef? SelectedControl => state.Selection is { } control ? new ControlRef(control) : null;

    public SelectionState? SelectionState
    {
        get
        {
            if (state.Selection is not { } selection)
                return null;
            var capabilities = state.Capabilities;
            BindingState binding = state.Action(selection) switch
            {
                null => new BindingState.Unknown(),
                KeyAction.UnassignedAction => new BindingState.Nothing(),
                KeyAction.Keys { Macro: var macro } => new BindingState.Presses(
                    [.. macro.Strokes.Select(ActionText.Stroke)],
                    macro.Delay,
                    capabilities?.Presses(macro) ?? macro.Strokes.Count,
                    capabilities?.PressLimit ?? 0),
                KeyAction.Media { Key: var key } => key.Choice() is { } choice
                    ? new BindingState.Media(choice)
                    : new BindingState.OtherMedia(key.Usage),
                KeyAction.Mouse { Action: var mouse } => new BindingState.Mouse(mouse.Choice()),
                _ => new BindingState.Unknown(),
            };
            int? layerNumber = state.LayerCount > 1 ? state.Layer + 1 : null;
            return new SelectionState(new ControlRef(selection).Title(), layerNumber, binding);
        }
    }

    /// <summary>What the selected key does, for marking the page's matching button.</summary>
    public ActionCaption? SelectedCaption =>
        state.Selection is { } control ? ActionCaption.Of(state.Action(control)) : null;

    // Keys page

    public IReadOnlyList<ModifierKey> HeldModifiers =>
        [.. ModifierKeys.All.Where(modifier => state.Held.HasFlag(modifier.ToModifiers()))];

    /// <summary>"Ctrl+Win", or null when none is held.</summary>
    public string? HeldModifierText => state.Held == Modifiers.None ? null : ActionText.Modifiers(state.Held);

    public int PressLimit => state.Capabilities?.PressLimit ?? 0;

    /// <summary>Whether each modifier takes a place of its own in a key, as on the standard format.</summary>
    public bool ModifiersCountAsPresses => state.Capabilities?.ModifiersTakePlaces ?? false;

    /// <summary>
    /// How the keyboard's vendor tool lays out its keys — the Windows tool's groups for the 8890, the cross-platform
    /// app's full-size keyboard for the rest — until the user picks one.
    /// </summary>
    public KeyArrangement VendorKeyArrangement =>
        state.Capabilities?.Commands is CommandSet.Packets ? KeyArrangement.Alphabetical : KeyArrangement.Qwerty;

    /// <summary>Every key, before a keyboard is identified.</summary>
    private bool Takes(KeyUsage key) => state.Capabilities is not { } capabilities || capabilities.Takes(key.Usage);

    /// <summary>The full-size keyboard. A key the keyboard isn't given keeps its place but is off.</summary>
    public IReadOnlyList<BoardCap> KeyBoard =>
    [
        .. FullKeyboard.Places.Select(place => new BoardCap(
            place.Face,
            place.Row,
            place.Column,
            place.Width,
            place.Face is not BoardFace.Key { Usage: var key } || Takes(key))),
    ];

    /// <summary>
    /// The keys a full-size keyboard doesn't have — F13–F24 and the two non-US keys — that the keyboard is given.
    /// </summary>
    public IReadOnlyList<KeyUsage> MoreKeys =>
        [.. KeyUsage.ExtraFunctionKeys.Concat(KeyUsage.International).Where(Takes)];

    /// <summary>
    /// The alphabetical arrangement: groups of the keys the keyboard is given, leaving out empty ones.
    /// </summary>
    public IReadOnlyList<KeyGroup> KeyGroups =>
    [
        .. KeyGroupKinds.All
            .Select(kind => new KeyGroup(kind, [.. kind.Keys().Where(Takes)]))
            .Where(group => !group.Keys.IsEmpty),
    ];

    /// <summary>Digits and symbols with their shifted characters, sent as Shift and the key.</summary>
    public IReadOnlyList<KeyUsage> ShiftSymbols =>
        [.. KeyUsage.Digits.Concat(KeyUsage.Symbols).Where(key => key.Shifted is not null && Takes(key))];

    // Media page

    public IReadOnlyList<MediaChoice> MediaChoices =>
        [.. (state.Capabilities?.MediaKeys ?? MediaKey.All).Select(key => key.Choice()).OfType<MediaChoice>()];

    // LED page

    public LightingPage? LightingPage
    {
        get
        {
            if (state.Capabilities is not { } capabilities)
                return null;
            var current = state.Lighting;
            switch (capabilities.Lighting)
            {
                case LightingSupport.Modes { Values: var modes }:
                    return new LightingPage.ModesOnly(
                        [.. modes], current is LayerLighting.ModeOnly { Mode: var mode } ? mode : null);
                case LightingSupport.ColorAndMode { ModeRange: var range }:
                    var colorMode = current as LayerLighting.ColorMode;
                    return new LightingPage.ColorAndMode(
                        [.. LedColors.All], Modes(range), colorMode?.Color.ToLedColor(), colorMode?.Mode);
                case LightingSupport.PerKeyColor { ModeRange: var range, Keys: var keys }:
                    KeyColorSelection? selected = state.Selection is { } control && state.KeyColor(control) is { } color
                        ? new KeyColorSelection(new ControlRef(control).Title(), Rgb.Of(color))
                        : null;
                    return new LightingPage.PerKeyColor(Modes(range), current?.Mode, keys, selected);
                default:
                    return null;
            }
        }
    }

    private static EquatableList<byte> Modes(InclusiveRange<byte> range) =>
        [.. Enumerable.Range(range.First, range.Last - range.First + 1).Select(mode => (byte)mode)];

    // Delay page

    public DelayPage DelayPage
    {
        get
        {
            switch (state.Capabilities?.Delays)
            {
                case DelaySupport.PerPress { Range: var range }:
                    EquatableList<PressDelay> presses = state.SelectedAction is KeyAction.Keys { Macro: var macro }
                        ? [.. macro.Strokes.Select((stroke, index) =>
                            new PressDelay(index, ActionText.Stroke(stroke), stroke.Delay))]
                        : [];
                    return new DelayPage.PerPress(new DelayRange(range.First, range.Last), presses);
                case DelaySupport.PerKey { Range: var range }:
                    uint? value = state.Selection is null
                        ? null
                        : state.SelectedAction is KeyAction.Keys { Macro: var keys } ? keys.Delay : 0;
                    return new DelayPage.PerKey(new DelayRange(range.First, range.Last), value);
                default:
                    return new DelayPage.Unavailable();
            }
        }
    }

    // Layout sheet

    public LayoutSheetState? LayoutSheet
    {
        get
        {
            if (state.ResolvedLayout is not { } resolved || state.Keyboard is not { } keyboard)
                return null;
            LayoutSheetMode mode;
            if (resolved.Source == Source.Identified)
            {
                mode = new LayoutSheetMode.Fixed();
            }
            else if (resolved.Grid is { } grid)
            {
                var shape = GridShape.FromId(resolved.Layout.Id) ?? grid.Assumed();
                mode = new LayoutSheetMode.Arrangement(
                    shape.Keys, shape.Columns, shape.Knobs, grid.MaxKeys, grid.MaxKnobs);
            }
            else
            {
                var choices = resolved.Choices
                    .Select(layout => LayoutGeometry.Of(layout) is { } geometry
                        ? new LayoutChoice(layout.Id, layout.KeyCount, layout.KnobCount, ShapeDrawing.Of(geometry))
                        : null)
                    .OfType<LayoutChoice>();
                mode = new LayoutSheetMode.Catalog([.. choices], resolved.Layout.Id);
            }
            var identity = state.Identification?.Identity;
            return new LayoutSheetState(
                keyboard.Usb.ToString(),
                identity?.KeyCount,
                identity?.KnobCount,
                mode,
                resolved.Rotation.QuarterTurns(),
                resolved.NeedsChoice);
        }
    }

    /// <summary>
    /// One of the app's layouts by its name, or with null the keyboard's own, drawn small and turned.
    /// </summary>
    public ShapeDrawing? PreviewDrawing(string? layoutId, int quarterTurns)
    {
        var layout = (layoutId is null ? null : KeyboardLayout.Named(layoutId)) ?? state.ResolvedLayout?.Layout;
        if (layout is null || LayoutGeometry.Of(layout) is not { } geometry)
            return null;
        return ShapeDrawing.Of(geometry.Rotated(Rotations.FromQuarterTurns(quarterTurns)));
    }

    /// <summary>An arrangement drawn small and turned; null when it is not one a keyboard can have.</summary>
    public ShapeDrawing? PreviewArrangement(byte keys, byte columns, byte knobs, int quarterTurns) =>
        GridShape.Create(keys, columns, knobs) is { } shape
            ? ShapeDrawing.Of(LayoutGeometry.Arrangement(shape).Rotated(Rotations.FromQuarterTurns(quarterTurns)))
            : null;
}
