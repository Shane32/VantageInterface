using System.Collections;

namespace VantageInterface;

/// <summary>
/// A set implementation optimized for concurrently enumerating a short list of elements.
/// Adding or removing elements is a slow O(N) operation, and may not immediately be
/// reflected within threads running on separate processors in a multi-processor system.
/// Additionally, changes to the set will not be reflected in
/// any concurrently running enumerations.
/// </summary>
public class ConcurrentSet<T> : IEnumerable<T>
{
    private volatile T[] _items = Array.Empty<T>();
    private readonly object _syncLock = new();

    /// <summary>
    /// Adds an object to the <see cref="ConcurrentSet{T}"/>.
    /// </summary>
    public void Add(T item)
    {
        lock (_syncLock) {
            int index = Array.IndexOf(_items, item);
            if (index == -1) {
                var newItems = new T[_items.Length + 1];
                Array.Copy(_items, newItems, _items.Length);
                newItems[_items.Length] = item;
                _items = newItems;
            }
        }
    }

    /// <summary>
    /// Removes an object from the <see cref="ConcurrentSet{T}"/>, returning <see langword="true"/> if the object was successfully removed.
    /// </summary>
    public bool Remove(T item)
    {
        lock (_syncLock) {
            int index = Array.IndexOf(_items, item);
            if (index == -1) {
                return false;
            }

            var newItems = new T[_items.Length - 1];
            Array.Copy(_items, 0, newItems, 0, index);
            Array.Copy(_items, index + 1, newItems, index, _items.Length - index - 1);
            _items = newItems;
            return true;
        }
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
}
