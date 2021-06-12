using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pow3r
{
    public sealed class PowerState
    {
        public const int MaxTickData = 180;

        public static readonly JsonSerializerOptions SerializerOptions = new()
        {
            IncludeFields = true,
            Converters = {new NodeIdJsonConverter()}
        };

        public Dictionary<NodeId, Supply> Supplies = new();
        public Dictionary<NodeId, Network> Networks = new();
        public Dictionary<NodeId, Load> Loads = new();
        public Dictionary<NodeId, Battery> Batteries = new();

        public readonly struct NodeId : IEquatable<NodeId>
        {
            public readonly int Id;

            public NodeId(int id)
            {
                Id = id;
            }

            public bool Equals(NodeId other)
            {
                return Id == other.Id;
            }

            public override bool Equals(object obj)
            {
                return obj is NodeId other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Id;
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
                return Id.ToString();
            }
        }

        public sealed class NodeIdJsonConverter : JsonConverter<NodeId>
        {
            public override NodeId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return new(reader.GetInt32());
            }

            public override void Write(Utf8JsonWriter writer, NodeId value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value.Id);
            }
        }

        public sealed class Supply
        {
            public readonly NodeId Id;

            // == Static parameters ==
            public bool Enabled = true;
            public float MaxSupply;

            public float SupplyRampRate;
            public float SupplyRampTolerance;

            // == Runtime parameters ==

            // Actual power supplied last network update.
            public float CurrentSupply;

            // The amount of power we WANT to be supplying to match grid load.
            public float SupplyRampTarget;

            // Position of the supply ramp.
            public float SupplyRampPosition;

            [JsonIgnore] public NodeId LinkedNetwork;

            // In-tick max supply thanks to ramp. Used during calculations.
            [JsonIgnore] public float EffectiveMaxSupply;

            // == Display ==
            [JsonIgnore] public Vector2 CurrentWindowPos;
            [JsonIgnore] public readonly float[] SuppliedPowerData = new float[MaxTickData];

            public Supply(NodeId id)
            {
                Id = id;
            }
        }

        public sealed class Load
        {
            public readonly NodeId Id;

            // == Static parameters ==
            public bool Enabled = true;
            public float DesiredPower;

            // == Runtime parameters ==
            public float ReceivingPower;

            [JsonIgnore] public NodeId LinkedNetwork;

            // == Display ==
            [JsonIgnore] public Vector2 CurrentWindowPos;
            [JsonIgnore] public readonly float[] ReceivedPowerData = new float[MaxTickData];

            public Load(NodeId id)
            {
                Id = id;
            }
        }

        public sealed class Battery
        {
            public readonly NodeId Id;

            // == Static parameters ==
            public bool Enabled;
            public float Capacity;
            public float MaxChargeRate;
            public float MaxThroughput; // 0 = infinite cuz imgui
            public float MaxSupply;
            public float SupplyRampTolerance;
            public float SupplyRampRate;

            // == Runtime parameters ==
            public float SupplyRampPosition;
            public float CurrentSupply;
            public float CurrentStorage;

            [JsonIgnore] public NodeId LinkedNetworkLoading;
            [JsonIgnore] public NodeId LinkedNetworkSupplying;

            // == Display ==
            [JsonIgnore] public Vector2 CurrentWindowPos;
            [JsonIgnore] public readonly float[] SuppliedPowerData = new float[MaxTickData];
            [JsonIgnore] public readonly float[] StoredPowerData = new float[MaxTickData];

            public Battery(NodeId id)
            {
                Id = id;
            }
        }

        // Readonly breaks json serialization.
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
        public sealed class Network
        {
            public readonly NodeId Id;

            public List<NodeId> Supplies = new();

            public List<NodeId> Loads = new();

            // "Loading" means the network is connected to the INPUT port of the battery.
            public List<NodeId> BatteriesLoading = new();

            // "Supplying" means the network is connected to the OUTPUT port of the battery.
            public List<NodeId> BatteriesSupplying = new();

            // Calculation parameters
            [JsonIgnore] public float LocalDemandTotal;
            [JsonIgnore] public float LocalDemandMet;
            [JsonIgnore] public float GroupDemandTotal;
            [JsonIgnore] public float GroupDemandMet;

            // Supply available this tick.
            [JsonIgnore] public float AvailableSupplyTotal;

            // Max theoretical supply assuming max ramp.
            [JsonIgnore] public float TheoreticalSupplyTotal;
            public float RemainingDemand => LocalDemandTotal - LocalDemandMet;

            [JsonIgnore] public Vector2 CurrentWindowPos;

            public Network(NodeId id)
            {
                Id = id;
            }
        }
    }
}
