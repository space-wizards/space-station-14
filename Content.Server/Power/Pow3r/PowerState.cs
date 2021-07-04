using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.Pow3r
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

            public override bool Equals(object? obj)
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
            [ViewVariables] public NodeId Id;

            // == Static parameters ==
            [ViewVariables(VVAccess.ReadWrite)] public bool Enabled = true;
            [ViewVariables(VVAccess.ReadWrite)] public bool Paused;
            [ViewVariables(VVAccess.ReadWrite)] public float MaxSupply;

            [ViewVariables(VVAccess.ReadWrite)] public float SupplyRampRate;
            [ViewVariables(VVAccess.ReadWrite)] public float SupplyRampTolerance;

            // == Runtime parameters ==

            // Actual power supplied last network update.
            [ViewVariables(VVAccess.ReadWrite)] public float CurrentSupply;

            // The amount of power we WANT to be supplying to match grid load.
            [ViewVariables(VVAccess.ReadWrite)] [JsonIgnore]
            public float SupplyRampTarget;

            // Position of the supply ramp.
            [ViewVariables(VVAccess.ReadWrite)] public float SupplyRampPosition;

            [ViewVariables] [JsonIgnore] public NodeId LinkedNetwork;

            // In-tick max supply thanks to ramp. Used during calculations.
            [JsonIgnore] public float EffectiveMaxSupply;
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
            [ViewVariables(VVAccess.ReadWrite)] public float SupplyRampTolerance;
            [ViewVariables(VVAccess.ReadWrite)] public float SupplyRampRate;
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

            [ViewVariables(VVAccess.ReadWrite)] [JsonIgnore]
            public bool LoadingDemandMarked;

            [ViewVariables(VVAccess.ReadWrite)] [JsonIgnore]
            public float TempMaxSupply;

            [ViewVariables(VVAccess.ReadWrite)] [JsonIgnore]
            public float DesiredPower;

            [ViewVariables(VVAccess.ReadWrite)] [JsonIgnore]
            public float SupplyRampTarget;

            [ViewVariables(VVAccess.ReadWrite)] [JsonIgnore]
            public NodeId LinkedNetworkCharging;

            [ViewVariables(VVAccess.ReadWrite)] [JsonIgnore]
            public NodeId LinkedNetworkDischarging;
        }

        // Readonly breaks json serialization.
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
        public sealed class Network
        {
            [ViewVariables] public NodeId Id;

            [ViewVariables] public List<NodeId> Supplies = new();

            [ViewVariables] public List<NodeId> Loads = new();

            // "Loading" means the network is connected to the INPUT port of the battery.
            [ViewVariables] public List<NodeId> BatteriesCharging = new();

            // "Supplying" means the network is connected to the OUTPUT port of the battery.
            [ViewVariables] public List<NodeId> BatteriesDischarging = new();

            // Calculation parameters used by GraphWalkSolver.
            // Unused by BatteryRampPegSolver.
            [JsonIgnore] public float LocalDemandTotal;
            [JsonIgnore] public float LocalDemandMet;
            [JsonIgnore] public float GroupDemandTotal;
            [JsonIgnore] public float GroupDemandMet;

            [ViewVariables] [JsonIgnore] public int Height;
            [JsonIgnore] public bool HeightTouched;

            // Supply available this tick.
            [JsonIgnore] public float AvailableSupplyTotal;

            // Max theoretical supply assuming max ramp.
            [JsonIgnore] public float TheoreticalSupplyTotal;
            public float RemainingDemand => LocalDemandTotal - LocalDemandMet;
        }
    }
}
