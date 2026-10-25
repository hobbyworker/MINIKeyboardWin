using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace MINIKeyboard.Domain;

/// <summary>
/// An immutable list compared by its items, so the records that hold one compare by value.
/// </summary>
[CollectionBuilder(typeof(EquatableArray), nameof(EquatableArray.Create))]
public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
{
    private readonly ImmutableArray<T> items;

    public EquatableArray(IEnumerable<T> items) => this.items = [.. items];

    private ImmutableArray<T> Items => items.IsDefault ? [] : items;

    public int Count => Items.Length;

    public T this[int index] => Items[index];

    public bool IsEmpty => Items.IsEmpty;

    public EquatableArray<T> SetItem(int index, T item) => new(Items.SetItem(index, item));

    public EquatableArray<T> Add(T item) => new(Items.Add(item));

    public EquatableArray<T> RemoveLast() => new(Items.RemoveAt(Items.Length - 1));

    public bool Equals(EquatableArray<T> other) => Items.SequenceEqual(other.Items);

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in Items)
            hash.Add(item);
        return hash.ToHashCode();
    }

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);

    public ImmutableArray<T>.Enumerator GetEnumerator() => Items.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)Items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Items).GetEnumerator();

    public override string ToString() => $"[{string.Join(", ", Items)}]";
}

public static class EquatableArray
{
    public static EquatableArray<T> Create<T>(ReadOnlySpan<T> items) => new(items.ToArray());
}
