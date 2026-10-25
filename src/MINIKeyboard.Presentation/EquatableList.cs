using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace MINIKeyboard.Presentation;

/// <summary>
/// An immutable list compared by its items, so the values the views draw compare by value — the presentation's own,
/// so the views never need the domain's.
/// </summary>
[CollectionBuilder(typeof(EquatableList), nameof(EquatableList.Create))]
public readonly struct EquatableList<T> : IEquatable<EquatableList<T>>, IReadOnlyList<T>
{
    private readonly ImmutableArray<T> items;

    public EquatableList(IEnumerable<T> items) => this.items = [.. items];

    private ImmutableArray<T> Items => items.IsDefault ? [] : items;

    public int Count => Items.Length;

    public T this[int index] => Items[index];

    public bool IsEmpty => Items.IsEmpty;

    public bool Equals(EquatableList<T> other) => Items.SequenceEqual(other.Items);

    public override bool Equals(object? obj) => obj is EquatableList<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in Items)
            hash.Add(item);
        return hash.ToHashCode();
    }

    public static bool operator ==(EquatableList<T> left, EquatableList<T> right) => left.Equals(right);

    public static bool operator !=(EquatableList<T> left, EquatableList<T> right) => !left.Equals(right);

    public ImmutableArray<T>.Enumerator GetEnumerator() => Items.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)Items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Items).GetEnumerator();

    public override string ToString() => $"[{string.Join(", ", Items)}]";
}

public static class EquatableList
{
    public static EquatableList<T> Create<T>(ReadOnlySpan<T> items) => new(items.ToArray());
}
