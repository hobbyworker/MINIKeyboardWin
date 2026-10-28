namespace MINIKeyboard.Domain;

/// <summary>Some controls of one layer, in control order.</summary>
/// <param name="Layer">From 0.</param>
public readonly record struct LayerControls(int Layer, EquatableArray<ControlId> Controls);

/// <summary>What reading one layer of a keyboard brought.</summary>
/// <param name="actions">The actions read, by control.</param>
/// <param name="unread">
/// The controls the read asked for that weren't read, in control order — empty when everything came.
/// </param>
/// <param name="outside">
/// The layout's controls the read doesn't ask for, in control order: they are neither read nor unread.
/// </param>
public sealed class LayerRead(
    IReadOnlyDictionary<ControlId, KeyAction> actions,
    EquatableArray<ControlId> unread,
    EquatableArray<ControlId> outside = default)
{
    public IReadOnlyDictionary<ControlId, KeyAction> Actions { get; } = actions;

    public EquatableArray<ControlId> Unread { get; } = unread;

    public EquatableArray<ControlId> Outside { get; } = outside;
}

/// <summary>One layer of what the app holds for a keyboard.</summary>
public sealed class Layer : IEquatable<Layer>
{
    internal Dictionary<ControlId, KeyAction> Actions { get; } = [];

    internal LayerLighting? Lighting { get; set; }

    internal SortedSet<ControlId> ChangedControls { get; } = [];

    public bool LightingChanged { get; internal set; }

    internal Layer Clone()
    {
        var copy = new Layer { Lighting = Lighting, LightingChanged = LightingChanged };
        foreach (var (control, action) in Actions)
            copy.Actions[control] = action;
        copy.ChangedControls.UnionWith(ChangedControls);
        return copy;
    }

    public bool Equals(Layer? other) =>
        other is not null
        && Actions.Count == other.Actions.Count
        && Actions.All(pair => other.Actions.TryGetValue(pair.Key, out var action) && action == pair.Value)
        && Equals(Lighting, other.Lighting)
        && ChangedControls.SetEquals(other.ChangedControls)
        && LightingChanged == other.LightingChanged;

    public override bool Equals(object? obj) => Equals(obj as Layer);

    public override int GetHashCode() => HashCode.Combine(Actions.Count, Lighting, ChangedControls.Count, LightingChanged);
}

/// <summary>
/// What the app holds for a keyboard: each layer's actions and lights, and what changed since they were read or saved.
/// What the app doesn't know — a keyboard that can't be read, a key nobody has set — is missing rather than guessed,
/// and is never written.
/// </summary>
public sealed class KeyboardConfiguration : IEquatable<KeyboardConfiguration>
{
    private readonly Layer[] layers;

    public KeyboardConfiguration(int layerCount) =>
        layers = Enumerable.Range(0, Math.Max(layerCount, 1)).Select(_ => new Layer()).ToArray();

    private KeyboardConfiguration(Layer[] layers) => this.layers = layers;

    public int LayerCount => layers.Length;

    public IReadOnlyList<Layer> Layers => layers;

    public KeyboardConfiguration Clone() => new(layers.Select(layer => layer.Clone()).ToArray());

    /// <summary>The action of <paramref name="control"/>, or null when the app doesn't know it.</summary>
    public KeyAction? Action(ControlId control, int layer) =>
        layers[layer].Actions.GetValueOrDefault(control);

    public LayerLighting? Lighting(int layer) => layers[layer].Lighting;

    public bool IsChanged(ControlId control, int layer) => layers[layer].ChangedControls.Contains(control);

    public bool HasUnsavedChanges => layers.Any(layer => layer.ChangedControls.Count > 0 || layer.LightingChanged);

    /// <summary>Sets an action to be written on the next save.</summary>
    public void Set(KeyAction action, ControlId control, int layer)
    {
        layers[layer].Actions[control] = action;
        layers[layer].ChangedControls.Add(control);
    }

    /// <summary>Sets a layer's lights to be written on the next save.</summary>
    public void SetLighting(LayerLighting lighting, int layer)
    {
        layers[layer].Lighting = lighting;
        layers[layer].LightingChanged = true;
    }

    /// <summary>
    /// What the keyboard said it holds. It replaces the app's values, saved or not, as the vendor app does when it
    /// reads.
    /// </summary>
    public void ApplyRead(IReadOnlyDictionary<ControlId, KeyAction> actions, int layer)
    {
        foreach (var (control, action) in actions)
        {
            layers[layer].ChangedControls.Remove(control);
            layers[layer].Actions[control] = action;
        }
    }

    public void ApplyReadLighting(LayerLighting lighting, int layer)
    {
        layers[layer].Lighting = lighting;
        layers[layer].LightingChanged = false;
    }

    /// <summary>Everything was written.</summary>
    public void MarkSaved() => MarkSaved([], []);

    /// <summary>
    /// Everything was written, but <paramref name="exceptControls"/> and the lights of the layers in
    /// <paramref name="exceptLighting"/> read back otherwise: those stay changed, with the app's values, to be saved
    /// again.
    /// </summary>
    public void MarkSaved(EquatableArray<LayerControls> exceptControls, EquatableArray<int> exceptLighting)
    {
        for (var index = 0; index < layers.Length; index++)
        {
            var layer = layers[index];
            layer.ChangedControls.IntersectWith(
                exceptControls.Where(entry => entry.Layer == index).SelectMany(entry => entry.Controls));
            layer.LightingChanged &= exceptLighting.Contains(index);
        }
    }

    public bool Equals(KeyboardConfiguration? other) => other is not null && layers.SequenceEqual(other.layers);

    public override bool Equals(object? obj) => Equals(obj as KeyboardConfiguration);

    public override int GetHashCode() => layers.Length;
}
