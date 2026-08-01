using System.Collections;
using System.Collections.Concurrent;
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
        public List<NetworkGroup>? GroupedNets; // DS14

        // DS14-start
        private readonly ConcurrentQueue<NodeId> _dirtyLoads = new();
        private readonly ConcurrentQueue<NodeId> _changedLoads = new();
        private readonly ConcurrentQueue<NodeId> _changedBatterySupply = new();
        private readonly ConcurrentQueue<NodeId> _changedBatteryStorage = new();
        // DS14-end

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
            public int Capacity => _storage.Length; // DS14

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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Contains(NodeId id)
            {
                if (id.Index < 0 || id.Index >= _storage.Length)
                    return false;

                ref var slot = ref _storage[id.Index];
                return slot.Generation == id.Generation && slot.NextSlot < 0;
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
                _storage[^1].Generation = 1; // DS14

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

        // DS14-start
        public sealed class NetworkGroup
        {
            public readonly List<Network> Networks = new();
            public int WorkCost;
        }

        public void AttachLoad(Load load)
        {
            load.Owner = this;
            MarkLoadDirty(load.Id);
        }

        public void DetachLoad(Load load)
        {
            load.Owner = null;
        }

        public void AttachSupply(Supply supply)
        {
            supply.Owner = this;
        }

        public void DetachSupply(Supply supply)
        {
            supply.Owner = null;
        }

        public void AttachBattery(Battery battery)
        {
            battery.Owner = this;
            MarkBatterySupplyChanged(battery.Id);
            MarkBatteryStorageChanged(battery.Id);
        }

        public void DetachBattery(Battery battery)
        {
            battery.Owner = null;
        }

        public void MarkLoadDirty(NodeId id)
        {
            if (id == default)
                return;

            _dirtyLoads.Enqueue(id);
            _changedLoads.Enqueue(id);

            if (!Loads.Contains(id))
                return;

            var load = Loads[id];
            if (Networks.Contains(load.LinkedNetwork))
                Networks[load.LinkedNetwork].LoadDemandDirty = true;
        }

        public void FlushDirtyLoads()
        {
            while (_dirtyLoads.TryDequeue(out var id))
            {
                if (id == default || !Loads.Contains(id))
                    continue;

                var load = Loads[id];
                if (Networks.Contains(load.LinkedNetwork))
                    Networks[load.LinkedNetwork].LoadDemandDirty = true;
            }
        }

        public void MarkLoadOutputChanged(NodeId id)
        {
            if (id != default)
                _changedLoads.Enqueue(id);
        }

        public void MarkSupplyDirty(NodeId id)
        {
            if (id == default || !Supplies.Contains(id))
                return;

            var supply = Supplies[id];
            if (Networks.Contains(supply.LinkedNetwork))
                Networks[supply.LinkedNetwork].SupplyDirty = true;
        }

        public void MarkBatteryDirty(NodeId id)
        {
            if (id == default || !Batteries.Contains(id))
                return;

            var battery = Batteries[id];

            if (Networks.Contains(battery.LinkedNetworkCharging))
                Networks[battery.LinkedNetworkCharging].BatteryLoadDirty = true;

            if (Networks.Contains(battery.LinkedNetworkDischarging))
                Networks[battery.LinkedNetworkDischarging].BatterySupplyDirty = true;
        }

        public void MarkBatterySupplyChanged(NodeId id)
        {
            if (id != default)
                _changedBatterySupply.Enqueue(id);
        }

        public void MarkBatteryStorageChanged(NodeId id)
        {
            if (id != default)
                _changedBatteryStorage.Enqueue(id);
        }

        public bool TryDequeueChangedLoad(out NodeId id)
        {
            return _changedLoads.TryDequeue(out id);
        }

        public bool TryDequeueChangedBatterySupply(out NodeId id)
        {
            return _changedBatterySupply.TryDequeue(out id);
        }

        public bool TryDequeueChangedBatteryStorage(out NodeId id)
        {
            return _changedBatteryStorage.TryDequeue(out id);
        }
        // DS14-end

        public sealed class Supply
        {
            [ViewVariables] public NodeId Id;
            // DS14-start
            [JsonIgnore] internal PowerState? Owner;

            private bool _enabled = true;
            private bool _paused;
            private float _maxSupply;
            private float _supplyRampRate = 5000;
            private float _supplyRampTolerance = 5000;
            private float _currentSupply;
            private float _supplyRampTarget;
            private float _supplyRampPosition;
            // DS14-end

            // == Static parameters ==
            // DS14-start
            [ViewVariables(VVAccess.ReadWrite)]
            public bool Enabled
            {
                get => _enabled;
                set
                {
                    if (_enabled == value)
                        return;
                    _enabled = value;
                    Owner?.MarkSupplyDirty(Id);
                }
            }
            [ViewVariables(VVAccess.ReadWrite)]
            public bool Paused
            {
                get => _paused;
                set
                {
                    if (_paused == value)
                        return;
                    _paused = value;
                    Owner?.MarkSupplyDirty(Id);
                }
            }
            [ViewVariables(VVAccess.ReadWrite)]
            public float MaxSupply
            {
                get => _maxSupply;
                set
                {
                    if (_maxSupply == value)
                        return;
                    _maxSupply = value;
                    Owner?.MarkSupplyDirty(Id);
                }
            }
            [ViewVariables(VVAccess.ReadWrite)]
            public float SupplyRampRate
            {
                get => _supplyRampRate;
                set
                {
                    if (_supplyRampRate == value)
                        return;
                    _supplyRampRate = value;
                    Owner?.MarkSupplyDirty(Id);
                }
            }
            [ViewVariables(VVAccess.ReadWrite)]
            public float SupplyRampTolerance
            {
                get => _supplyRampTolerance;
                set
                {
                    if (_supplyRampTolerance == value)
                        return;
                    _supplyRampTolerance = value;
                    Owner?.MarkSupplyDirty(Id);
                }
            }
            // DS14-end

            // == Runtime parameters ==

            /// <summary>
            ///     Actual power supplied last network update.
            /// </summary>
            // DS14-start
            [ViewVariables(VVAccess.ReadWrite)]
            public float CurrentSupply
            {
                get => _currentSupply;
                set => _currentSupply = value;
            }
            // DS14-end

            /// <summary>
            ///     The amount of power we WANT to be supplying to match grid load.
            /// </summary>
            [ViewVariables(VVAccess.ReadWrite)] [JsonIgnore]
            // DS14-start
            public float SupplyRampTarget
            {
                get => _supplyRampTarget;
                set => _supplyRampTarget = value;
            }
            // DS14-end

            /// <summary>
            ///     Position of the supply ramp.
            /// </summary>
            // DS14-start
            [ViewVariables(VVAccess.ReadWrite)]
            public float SupplyRampPosition
            {
                get => _supplyRampPosition;
                set => _supplyRampPosition = value;
            }
            // DS14-end

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
            // DS14-start
            [JsonIgnore] internal PowerState? Owner;

            private bool _enabled = true;
            private bool _paused;
            private float _desiredPower;
            private float _receivingPower;
            // DS14-end

            // == Static parameters ==
            // DS14-start
            [ViewVariables(VVAccess.ReadWrite)]
            public bool Enabled
            {
                get => _enabled;
                set
                {
                    if (_enabled == value)
                        return;
                    _enabled = value;
                    Owner?.MarkLoadDirty(Id);
                }
            }

            [ViewVariables(VVAccess.ReadWrite)]
            public bool Paused
            {
                get => _paused;
                set
                {
                    if (_paused == value)
                        return;
                    _paused = value;
                    Owner?.MarkLoadDirty(Id);
                }
            }

            [ViewVariables(VVAccess.ReadWrite)]
            public float DesiredPower
            {
                get => _desiredPower;
                set
                {
                    if (_desiredPower == value)
                        return;
                    _desiredPower = value;
                    Owner?.MarkLoadDirty(Id);
                }
            }
            // DS14-end

            // == Runtime parameters ==
            // DS14-start
            [ViewVariables(VVAccess.ReadWrite)]
            public float ReceivingPower
            {
                get => _receivingPower;
                set => SetReceivingPower(value);
            }
            // DS14-end

            [ViewVariables] [JsonIgnore] public NodeId LinkedNetwork;

            // DS14-start
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetReceivingPower(float value)
            {
                if (_receivingPower == value)
                    return;
                _receivingPower = value;
                Owner?.MarkLoadOutputChanged(Id);
            }

            public void QueueUpdate()
            {
                Owner?.MarkLoadOutputChanged(Id);
            }
            // DS14-end
        }

        public sealed class Battery
        {
            [ViewVariables] public NodeId Id;
            // DS14-start
            [JsonIgnore] internal PowerState? Owner;

            private bool _enabled = true;
            private bool _paused;
            private bool _canDischarge = true;
            private bool _canCharge = true;
            private float _capacity;
            private float _maxChargeRate;
            private float _maxThroughput;
            private float _maxSupply;
            private float _supplyRampTolerance = 5000;
            private float _supplyRampRate = 5000;
            private float _efficiency = 1;
            private float _supplyRampPosition;
            private float _currentSupply;
            private float _currentStorage;
            private float _currentReceiving;
            private float _loadingNetworkDemand;
            // DS14-end

            // == Static parameters ==
            // DS14-start
            [ViewVariables(VVAccess.ReadWrite)]
            public bool Enabled
            {
                get => _enabled;
                set
                {
                    if (_enabled == value)
                        return;
                    _enabled = value;
                    Owner?.MarkBatteryDirty(Id);
                }
            }

            [ViewVariables(VVAccess.ReadWrite)]
            public bool Paused
            {
                get => _paused;
                set
                {
                    if (_paused == value)
                        return;
                    _paused = value;
                    Owner?.MarkBatteryDirty(Id);
                }
            }

            [ViewVariables(VVAccess.ReadWrite)]
            public bool CanDischarge
            {
                get => _canDischarge;
                set
                {
                    if (_canDischarge == value)
                        return;
                    _canDischarge = value;
                    Owner?.MarkBatteryDirty(Id);
                }
            }

            [ViewVariables(VVAccess.ReadWrite)]
            public bool CanCharge
            {
                get => _canCharge;
                set
                {
                    if (_canCharge == value)
                        return;
                    _canCharge = value;
                    Owner?.MarkBatteryDirty(Id);
                }
            }

            [ViewVariables(VVAccess.ReadWrite)]
            public float Capacity
            {
                get => _capacity;
                set
                {
                    if (_capacity == value)
                        return;
                    _capacity = value;
                    Owner?.MarkBatteryDirty(Id);
                }
            }

            [ViewVariables(VVAccess.ReadWrite)]
            public float MaxChargeRate
            {
                get => _maxChargeRate;
                set
                {
                    if (_maxChargeRate == value)
                        return;
                    _maxChargeRate = value;
                    Owner?.MarkBatteryDirty(Id);
                }
            }

            [ViewVariables(VVAccess.ReadWrite)]
            public float MaxThroughput
            {
                get => _maxThroughput;
                set
                {
                    if (_maxThroughput == value)
                        return;
                    _maxThroughput = value;
                    Owner?.MarkBatteryDirty(Id);
                }
            }

            [ViewVariables(VVAccess.ReadWrite)]
            public float MaxSupply
            {
                get => _maxSupply;
                set
                {
                    if (_maxSupply == value)
                        return;
                    _maxSupply = value;
                    Owner?.MarkBatteryDirty(Id);
                }
            }
            // DS14-end

            /// <summary>
            ///     The batteries supply ramp tolerance. This is an always available supply added to the ramped supply.
            /// </summary>
            /// <remarks>
            ///     Note that this MUST BE GREATER THAN ZERO, otherwise the current battery ramping calculation will not work.
            /// </remarks>
            // DS14-start
            [ViewVariables(VVAccess.ReadWrite)]
            public float SupplyRampTolerance
            {
                get => _supplyRampTolerance;
                set
                {
                    if (_supplyRampTolerance == value)
                        return;
                    _supplyRampTolerance = value;
                    Owner?.MarkBatteryDirty(Id);
                }
            }

            [ViewVariables(VVAccess.ReadWrite)]
            public float SupplyRampRate
            {
                get => _supplyRampRate;
                set
                {
                    if (_supplyRampRate == value)
                        return;
                    _supplyRampRate = value;
                    Owner?.MarkBatteryDirty(Id);
                }
            }

            [ViewVariables(VVAccess.ReadWrite)]
            public float Efficiency
            {
                get => _efficiency;
                set
                {
                    if (_efficiency == value)
                        return;
                    _efficiency = value;
                    Owner?.MarkBatteryDirty(Id);
                }
            }
            // DS14-end

            // == Runtime parameters ==
            // DS14-start
            [ViewVariables(VVAccess.ReadWrite)]
            public float SupplyRampPosition
            {
                get => _supplyRampPosition;
                set => _supplyRampPosition = value;
            }

            [ViewVariables(VVAccess.ReadWrite)]
            public float CurrentSupply
            {
                get => _currentSupply;
                set => SetCurrentSupply(value);
            }

            [ViewVariables(VVAccess.ReadWrite)]
            public float CurrentStorage
            {
                get => _currentStorage;
                set => SetCurrentStorage(value);
            }

            [ViewVariables(VVAccess.ReadWrite)]
            public float CurrentReceiving
            {
                get => _currentReceiving;
                set => _currentReceiving = value;
            }

            [ViewVariables(VVAccess.ReadWrite)]
            public float LoadingNetworkDemand
            {
                get => _loadingNetworkDemand;
                set => _loadingNetworkDemand = value;
            }
            // DS14-end

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

            // DS14-start
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetCurrentSupply(float value)
            {
                if (_currentSupply == value)
                    return;
                _currentSupply = value;
                Owner?.MarkBatterySupplyChanged(Id);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetCurrentStorage(float value, bool trackChange = true)
            {
                if (_currentStorage == value)
                    return;
                _currentStorage = value;
                if (trackChange)
                    Owner?.MarkBatteryStorageChanged(Id);
            }
            // DS14-end
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

            // DS14-start
            [ViewVariables] [JsonIgnore] public bool LoadDemandDirty = true;
            [ViewVariables] [JsonIgnore] public bool BatteryLoadDirty = true;
            [ViewVariables] [JsonIgnore] public bool BatterySupplyDirty = true;
            [ViewVariables] [JsonIgnore] public bool SupplyDirty = true;
            [ViewVariables] [JsonIgnore] public float CachedLoadDemand;
            [ViewVariables] [JsonIgnore] public float LastLoadSupplyRatio = float.NaN;
            // DS14-end
        }
    }
}
