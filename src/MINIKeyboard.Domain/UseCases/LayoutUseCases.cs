namespace MINIKeyboard.Domain.UseCases;

/// <summary>
/// The layout to draw for a newly identified keyboard. A keyboard that said how many keys and knobs it has gets the
/// layout the cross-platform app draws for that — its records follow it, so it is not the user's to change, only how
/// it is turned. A 884x keyboard that doesn't say gets the layout the user chose from the app's, and the 1189:8890 —
/// which that app doesn't know, and whose own tool draws no shape — the arrangement the user gave it
/// (<see cref="GridShape"/>). Until they choose: what the vendor tool would show.
/// </summary>
public sealed class ResolveLayoutUseCase(ILayoutProfileRepository profiles)
{
    public ResolvedLayout Execute(ConnectedKeyboard keyboard, KeyboardIdentification identification)
    {
        var saved = profiles.Profile(keyboard.Usb);
        var rotation = saved?.Rotation ?? Rotation.Upright;
        if (identification.Identity is { } identity
            && KeyboardLayout.Named(KeyboardLayout.IdentifiedId(identity, keyboard.Usb.ProductId)) is { } identified)
        {
            return new ResolvedLayout(identified, rotation, Source.Identified, [], null);
        }
        var capabilities = new KeyboardCapabilities(identification.Commands);
        if (capabilities.GridLimits is { } grid)
        {
            if (saved is not null && GridShape.FromId(saved.LayoutId) is { } shape && grid.Contains(shape))
                return new ResolvedLayout(shape.Layout(), saved.Rotation, Source.Chosen, [], grid);
            return new ResolvedLayout(grid.Assumed().Layout(), rotation, Source.Assumed, [], grid);
        }
        var choices = Choices(capabilities);
        if (saved is not null && choices.FirstOrDefault(layout => layout.Id == saved.LayoutId) is { } chosen)
            return new ResolvedLayout(chosen, saved.Rotation, Source.Chosen, choices, null);
        return new ResolvedLayout(Assumed(choices), rotation, Source.Assumed, choices, null);
    }

    /// <summary>Every layout the keyboard can address, fewest controls first.</summary>
    public static EquatableArray<KeyboardLayout> Choices(KeyboardCapabilities capabilities) =>
        new(KeyboardLayout.All
            .Where(capabilities.Addresses)
            .OrderBy(layout => layout.KeyCount)
            .ThenBy(layout => layout.KnobCount)
            .ThenBy(layout => layout.Id, StringComparer.Ordinal));

    /// <summary>
    /// What each vendor tool draws for a keyboard it knows nothing of: the cross-platform app's 15 keys and three
    /// knobs, or where the keyboard can't have that, the layout with the most controls — the Windows tool's 12 keys
    /// and two knobs.
    /// </summary>
    private static KeyboardLayout Assumed(EquatableArray<KeyboardLayout> choices) =>
        choices.FirstOrDefault(layout => layout.Id == KeyboardLayout.FallbackId)
        ?? choices
            .OrderByDescending(layout => layout.Controls.Count)
            .ThenBy(layout => layout.Id, StringComparer.Ordinal)
            .FirstOrDefault()
        ?? KeyboardLayout.Named(KeyboardLayout.FallbackId)
        ?? throw new InvalidOperationException("the cross-platform app's fallback layout is among its layouts");
}

/// <summary>Keeps the user's choice of layout and rotation for a keyboard.</summary>
public sealed class ChooseLayoutUseCase(ILayoutProfileRepository profiles)
{
    /// <summary>
    /// The resolved layout with <paramref name="layout"/> and <paramref name="rotation"/> in place. The layout of a
    /// keyboard that said its shape stays as it is; only the rotation is kept.
    /// </summary>
    public ResolvedLayout Execute(ResolvedLayout resolved, KeyboardLayout layout, Rotation rotation, UsbIdentity keyboard)
    {
        var allowed = resolved.Grid is { } grid
            ? GridShape.FromId(layout.Id) is { } shape && grid.Contains(shape)
            : resolved.Choices.Contains(layout);
        var chosen = resolved.Source == Source.Identified || !allowed ? resolved.Layout : layout;
        profiles.Save(new LayoutProfile(chosen.Id, rotation), keyboard);
        var source = resolved.Source == Source.Identified ? Source.Identified : Source.Chosen;
        return resolved with { Layout = chosen, Rotation = rotation, Source = source };
    }
}
