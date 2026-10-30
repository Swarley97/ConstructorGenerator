using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ConstructorGenerator.Model;

/// <summary>
/// Immutable array with structural equality so it can be used in incremental generator pipeline models.
/// </summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
{
    private readonly T[]? _items;

    public EquatableArray(IEnumerable<T> items)
    {
        _items = items.ToArray();
    }

    public static EquatableArray<T> Empty => default;

    public int Count => _items?.Length ?? 0;

    public T this[int index] => (_items ?? Array.Empty<T>())[index];

    public bool Equals(EquatableArray<T> other)
    {
        return (_items ?? Array.Empty<T>()).SequenceEqual(other._items ?? Array.Empty<T>());
    }

    public override bool Equals(object? obj)
    {
        return obj is EquatableArray<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            foreach (T item in this)
            {
                hash = hash * 31 + (item?.GetHashCode() ?? 0);
            }

            return hash;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)(_items ?? Array.Empty<T>())).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

internal static class EquatableArrayExtensions
{
    public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> items)
    {
        return new EquatableArray<T>(items);
    }
}
