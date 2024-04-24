using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Robust.Shared.Utility;
using static Content.Server.Power.Pow3r.PowerState;

namespace Content.Server.Power.Pow3r
{
    public sealed class PowerState
    {
        public static readonly JsonSerializerOptions SerializerOptions = new()
        {
            IncludeFields = true,
            Converters = {new NodeIdJsonConverter()}
        };

        public GenIdStorage<Supply> Supplies = new();
        public GenIdStorage<Network> Networks = new();
        public GenIdStorage<Load> Loads = new();
        public GenIdStorage<Battery> Batteries = new();
        public List<List<Network>>? GroupedNets;

        public readonly struct NodeId : IEquatable<NodeId>
        {
            public readonly int Index;
            public readonly int Generation;

            public long Combined => (uint) Index | ((long) Generation << 32);

            public NodeId(int index, int generation)
            {
                Index = index;
                Generation = generation;
            }

            public NodeId(long combined)
            {
                Index = (int) combined;
                Generation = (int) (combined >> 32);
            }

            public bool Equals(NodeId other)
            {
                return Index == other.Index && Generation == other.Generation;
            }

            public override bool Equals(object? obj)
            {
                return obj is NodeId other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Index, Generation);
            }

            public static bool operator ==(NodeId left, NodeId right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(NodeId left, NodeId right)
            {
                return !left.Equals(right);
            }

            public override string ToString()
            {
                return $"{Index} (G{Generation})";
            }
        }

        public static class GenIdStorage
        {
            public static GenIdStorage<T> FromEnumerable<T>(IEnumerable<(NodeId, T)> enumerable)
            {
                return GenIdStorage<T>.FromEnumerable(enumerable);
            }
        }

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

        public sealed class NodeIdJsonConverter : JsonConverter<NodeId>
        {
            public override NodeId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return new NodeId(reader.GetInt64());
            }

            public override void Write(Utf8JsonWriter writer, NodeId value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value.Combined);
            }
        }

        public sealed class Supply
        {
            [ViewVariables] public NodeId Id;

            // == Static parameters ==
            [ViewVariables(VVAccess.ReadWrite)] public bool Enabled = true;
            [ViewVariables(VVAccess.ReadWrite)] public bool Paused;
            [ViewVariables(VVAccess.ReadWrite)] public float MaxSupply;

            [ViewVariables(VVAccess.ReadWrite)] public float SupplyRampRate = 5000;
            [ViewVariables(VVAccess.ReadWrite)] public float SupplyRampTolerance = 5000;

            // == Runtime parameters ==

            /// <summary>
            ///     Actual power supplied last network update.
            /// </summary>
            [ViewVariables(VVAccess.ReadWrite)] public float CurrentSupply;

            /// <summary>
            ///     The amount of power we WANT to be supplying to match grid load.
            /// </summary>
            [ViewVariables(VVAccess.ReadWrite)] [JsonIgnore]
            public float SupplyRampTarget;

            /// <summary>
            ///     Position of the supply ramp.
            /// </summary>
            [ViewVariables(VVAccess.ReadWrite)] public float SupplyRampPosition;

            [ViewVariables] [JsonIgnore] public NodeId LinkedNetwork;

            /// <summary>
            ///     Supply available during a tick. The actual current supply will be less than or equal to this. Used
            ///     during calculations.
            /// </summary>
            [JsonIgnore] public float AvailableSupply;
        }

        public sealed class Load
        {
            [ViewVariables] public NodeId Id;

            // == Static parameters ==
            [ViewVariables(VVAccess.ReadWrite)] public bool Enabled = true;
            [ViewVariables(VVAccess.ReadWrite)] public bool Paused;
            [ViewVariables(VVAccess.ReadWrite)] public float DesiredPower;

            // == Runtime parameters ==
            [ViewVariables(VVAccess.ReadWrite)] public float ReceivingPower;

            [ViewVariables] [JsonIgnore] public NodeId LinkedNetwork;
        }

        public sealed class Battery
        {
            [ViewVariables] public NodeId Id;

            // == Static parameters ==
            [ViewVariables(VVAccess.ReadWrite)] public bool Enabled = true;
            [ViewVariables(VVAccess.ReadWrite)] public bool Paused;
            [ViewVariables(VVAccess.ReadWrite)] public bool CanDischarge = true;
            [ViewVariables(VVAccess.ReadWrite)] public bool CanCharge = true;
            [ViewVariables(VVAccess.ReadWrite)] public float Capacity;
            [ViewVariables(VVAccess.ReadWrite)] public float MaxChargeRate;
            [ViewVariables(VVAccess.ReadWrite)] public float MaxThroughput; // 0 = infinite cuz imgui
            [ViewVariables(VVAccess.ReadWrite)] public float MaxSupply;

            /// <summary>
            ///     The batteries supply ramp tolerance. This is an always available supply added to the ramped supply.
            /// </summary>
            /// <remarks>
            ///     Note that this MUST BE GREATER THAN ZERO, otherwise the current battery ramping calculation will not work.
            /// </remarks>
            [ViewVariables(VVAccess.ReadWrite)] public float SupplyRampTolerance = 5000;

            [ViewVariables(VVAccess.ReadWrite)] public float SupplyRampRate = 5000;
            [ViewVariables(VVAccess.ReadWrite)] public float Efficiency = 1;

            // == Runtime parameters ==
            [ViewVariables(VVAccess.ReadWrite)] public float SupplyRampPosition;
            [ViewVariables(VVAccess.ReadWrite)] public float CurrentSupply;
            [ViewVariables(VVAccess.ReadWrite)] public float CurrentStorage;
            [ViewVariables(VVAccess.ReadWrite)] public float CurrentReceiving;
            [ViewVariables(VVAccess.ReadWrite)] public float LoadingNetworkDemand;

            [ViewVariables(VVAccess.ReadWrite)] [JsonIgnore]
            public bool SupplyingMarked;

            [ViewVariables(VVAccess.ReadWrite)] [JsonIgnore]
            public bool LoadingMarked;

            /// <summary>
            ///     Amount of supply that the battery can provide this tick.
            /// </summary>
            [ViewVariables(VVAccess.ReadWrite)] [JsonIgnore]
            public float AvailableSupply;

            [ViewVariables(VVAccess.ReadWrite)] [JsonIgnore]
            public float DesiredPower;

            [ViewVariables(VVAccess.ReadWrite)] [JsonIgnore]
            public float SupplyRampTarget;

            [ViewVariables(VVAccess.ReadWrite)] [JsonIgnore]
            public NodeId LinkedNetworkCharging;

            [ViewVariables(VVAccess.ReadWrite)] [JsonIgnore]
            public NodeId LinkedNetworkDischarging;

            /// <summary>
            ///  Theoretical maximum effective supply, assuming the network providing power to this battery continues to supply it
            ///  at the same rate.
            /// </summary>
            [ViewVariables]
            public float MaxEffectiveSupply;
        }

        // Readonly breaks json serialization.
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
        public sealed class Network
        {
            [ViewVariables] public NodeId Id;

            /// <summary>
            ///     Power generators
            /// </summary>
            [ViewVariables] public List<NodeId> Supplies = new();

            /// <summary>
            ///     Power consumers.
            /// </summary>
            [ViewVariables] public List<NodeId> Loads = new();

            /// <summary>
            ///     Batteries that are draining power from this network (connected to the INPUT port of the battery).
            /// </summary>
            [ViewVariables] public List<NodeId> BatteryLoads = new();

            /// <summary>
            ///     Batteries that are supplying power to this network (connected to the OUTPUT port of the battery).
            /// </summary>
            [ViewVariables] public List<NodeId> BatterySupplies = new();

            /// <summary>
            ///     The total load on the power network as of last tick.
            /// </summary>
            [ViewVariables] public float LastCombinedLoad = 0f;

            /// <summary>
            ///     Available supply, including both normal supplies and batteries.
            /// </summary>
            [ViewVariables] public float LastCombinedSupply = 0f;

            /// <summary>
            ///     Theoretical maximum supply, including both normal supplies and batteries.
            /// </summary>
            [ViewVariables] public float LastCombinedMaxSupply = 0f;

            [ViewVariables] [JsonIgnore] public int Height;
        }
    }
}
