using System.Collections.Generic;
using System.Numerics;

namespace Pow3r
{
    internal sealed unsafe partial class Program
    {
        private sealed class Supply
        {
            public readonly int Id;

            // == Static parameters ==
            public bool Enabled = true;
            public float MaxSupply;

            public float SupplyRampRate;
            public float SupplyRampTolerance;

            // == Runtime parameters ==
            public Network LinkedNetwork;

            // Actual power supplied last network update.
            public float CurrentSupply;

            // In-tick max supply thanks to ramp. Used during calculations.
            public float EffectiveMaxSupply;

            // The amount of power we WANT to be supplying to match grid load.
            public float SupplyRampTarget;

            // Position of the supply ramp.
            public float SupplyRampPosition;

            // UI/vis stuff.
            public Vector2 CurrentWindowPos;
            public readonly float[] SuppliedPowerData = new float[MaxTickData];

            public Supply(int id)
            {
                Id = id;
            }
        }

        private sealed class Load
        {
            public readonly int Id;

            // == Static parameters ==
            public bool Enabled = true;
            public float DesiredPower;

            // == Runtime parameters ==
            public Network LinkedNetwork;
            public float ReceivingPower;

            // == Display ==
            public Vector2 CurrentWindowPos;
            public readonly float[] ReceivedPowerData = new float[MaxTickData];

            public Load(int id)
            {
                Id = id;
            }
        }

        private sealed class Battery
        {
            public readonly int Id;

            // == Static parameters ==
            public bool Enabled;
            public float Capacity;
            public float MaxChargeRate;
            public float MaxSupply;
            public float SupplyRampTolerance;
            public float SupplyRampRate;

            // == Runtime parameters ==
            public Network LinkedNetworkLoading;
            public Network LinkedNetworkSupplying;
            public float SupplyRampPosition;
            public float CurrentSupply;
            public float CurrentStorage;

            // == Display ==
            public Vector2 CurrentWindowPos;
            public readonly float[] SuppliedPowerData = new float[MaxTickData];
            public readonly float[] StoredPowerData = new float[MaxTickData];

            public Battery(int id)
            {
                Id = id;
            }
        }

        private sealed class Network
        {
            public readonly int Id;

            public readonly List<Supply> Supplies = new();

            public readonly List<Load> Loads = new();

            // "Loading" means the network is connected to the INPUT port of the battery.
            public readonly List<Battery> BatteriesLoading = new();

            // "Supplying" means the network is connected to the OUTPUT port of the battery.
            public readonly List<Battery> BatteriesSupplying = new();

            // Calculation parameters
            public float DemandTotal;
            public float MetDemand;
            public float AvailableSupplyTotal;
            public float TheoreticalSupplyTotal;
            public float RemainingDemand => DemandTotal - MetDemand;

            public int TreeHeight;

            public Vector2 CurrentWindowPos;

            public Network(int id)
            {
                Id = id;
            }
        }
    }
}
