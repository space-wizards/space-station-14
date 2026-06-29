using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using Content.Shared.Collections;
using Robust.Shared.Utility;

namespace Content.Server.Collections;

public static class GenIdStorage
{
    public static GenIdStorage<T> FromEnumerable<T>(IEnumerable<(NodeId, T)> enumerable)
    {
        return GenIdStorage<T>.FromEnumerable(enumerable);
    }
}

// TODO make this even more generic and more intuitive to use
public sealed class GenIdStorage<T>
{
    // This is an implementation of "generational index" storage.
    //
    // The advantage of this storage method is extremely fast, O(1) lookup (way faster than Dictionary).
    // Resolving a value in the storage is a single array load and generation compare. Extremely fast.
    // Indices can also be cached into temporary
    // Disadvantages are that storage cannot be shrunk, and sparse storage is inefficient space wise.
    // Also this implementation does not have optimizations necessary to make sparse iteration efficient.
    //
    // The idea here is that the index type (NodeId in this case) has both an index and a generation.
    // The index is an integer index into the storage array, the generation is used to avoid use-after-free.
    //
    // Empty slots in the array form a linked list of free slots.
    // When we allocate a new slot, we pop one link off this linked list and hand out its index + generation.
    //
    // When we free a node, we bump the generation of the slot and make it the head of the linked list.
    // The generation being bumped means that any IDs to this slot will fail to resolve (generation mismatch).
    //

    // Index of the next free slot to use when allocating a new one.
    // If this is int.MaxValue,
    // it basically means "no slot available" and the next allocation call should resize the array storage.
    private int _nextFree = int.MaxValue;
    private Slot[] _storage;

    public int Count { get; private set; }

    public ref T this[NodeId id]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ref var slot = ref _storage[id.Index];
            if (slot.Generation != id.Generation)
                ThrowKeyNotFound();

            return ref slot.Value;
        }
    }

    public GenIdStorage()
    {
        _storage = Array.Empty<Slot>();
    }

    public static GenIdStorage<T> FromEnumerable(IEnumerable<(NodeId, T)> enumerable)
    {
        var storage = new GenIdStorage<T>();

        // Cache enumerable to array to do double enumeration.
        var cache = enumerable.ToArray();

        if (cache.Length == 0)
            return storage;

        // Figure out max size necessary and set storage size to that.
        var maxSize = cache.Max(tup => tup.Item1.Index) + 1;
        storage._storage = new Slot[maxSize];

        // Fill in slots.
        foreach (var (id, value) in cache)
        {
            DebugTools.Assert(id.Generation != 0, "Generation cannot be 0");

            ref var slot = ref storage._storage[id.Index];
            DebugTools.Assert(slot.Generation == 0, "Duplicate key index!");

            slot.Generation = id.Generation;
            slot.Value = value;
            slot.NextSlot = -1;
        }

        // Go through empty slots and build the free chain.
        var nextFree = int.MaxValue;
        for (var i = 0; i < storage._storage.Length; i++)
        {
            ref var slot = ref storage._storage[i];

            if (slot.NextSlot == -1)
                // Slot in use.
                continue;

            slot.NextSlot = nextFree;
            nextFree = i;
        }

        storage.Count = cache.Length;
        storage._nextFree = nextFree;

        // Sanity check for a former bug with save/load.
        DebugTools.Assert(storage.Values.Count() == storage.Count);

        return storage;
    }

    public ref T Allocate(out NodeId id)
    {
        if (_nextFree == int.MaxValue)
            ReAllocate();

        var idx = _nextFree;
        ref var slot = ref _storage[idx];

        Count += 1;
        _nextFree = slot.NextSlot;
        // NextSlot = -1 indicates filled.
        slot.NextSlot = -1;

        id = new NodeId(idx, slot.Generation);
        return ref slot.Value;
    }

    public void Free(NodeId id)
    {
        var idx = id.Index;
        ref var slot = ref _storage[idx];
        if (slot.Generation != id.Generation)
            ThrowKeyNotFound();

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            slot.Value = default!;

        Count -= 1;
        slot.Generation += 1;
        slot.NextSlot = _nextFree;
        _nextFree = idx;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ReAllocate()
    {
        var oldLength = _storage.Length;
        var newLength = Math.Max(oldLength, 2) * 2;

        ReAllocateTo(newLength);
    }

    private void ReAllocateTo(int newSize)
    {
        var oldLength = _storage.Length;
        DebugTools.Assert(newSize >= oldLength, "Cannot shrink GenIdStorage");

        Array.Resize(ref _storage, newSize);

        for (var i = oldLength; i < newSize - 1; i++)
        {
            // Build linked list chain for newly allocated segment.
            ref var slot = ref _storage[i];
            slot.NextSlot = i + 1;
            // Every slot starts at generation 1.
            slot.Generation = 1;
        }

        _storage[^1].NextSlot = _nextFree;

        _nextFree = oldLength;
    }

    public ValuesCollection Values => new(this);

    private struct Slot
    {
        // Next link on the free list. if int.MaxValue then this is the tail.
        // If negative, this slot is occupied.
        public int NextSlot;

        // Generation of this slot.
        public int Generation;
        public T Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowKeyNotFound()
    {
        throw new KeyNotFoundException();
    }

    public readonly struct ValuesCollection : IReadOnlyCollection<T>
    {
        private readonly GenIdStorage<T> _owner;

        public ValuesCollection(GenIdStorage<T> owner)
        {
            _owner = owner;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_owner);
        }

        public int Count => _owner.Count;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<T>
        {
            // Save the array in the enumerator here to avoid a few pointer dereferences.
            private readonly Slot[] _owner;
            private int _index;

            public Enumerator(GenIdStorage<T> owner)
            {
                _owner = owner._storage;
                Current = default!;
                _index = -1;
            }

            public bool MoveNext()
            {
                while (true)
                {
                    _index += 1;
                    if (_index >= _owner.Length)
                        return false;

                    ref var slot = ref _owner[_index];

                    if (slot.NextSlot < 0)
                    {
                        Current = slot.Value;
                        return true;
                    }
                }
            }

            public void Reset()
            {
                _index = -1;
            }

            object IEnumerator.Current => Current!;

            public T Current { get; private set; }

            public void Dispose()
            {
            }
        }
    }
}
