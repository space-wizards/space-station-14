using Robust.Shared.Utility;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static Content.Server.Power.Pow3r.PowerState;

namespace Content.Server.Power.Pow3r
{
    public sealed class BatteryRampPegSolver : IPowerSolver
    {
        private sealed class HeightComparer : Comparer<Network>
        {
            public static HeightComparer Instance { get; } = new();

            public override int Compare(Network? x, Network? y)
            {
                if (x!.Height == y!.Height) return 0;
                if (x!.Height > y!.Height) return 1;
                return -1;
            }
        }

        public void Tick(float frameTime, PowerState state, int parallel)
        {
            ClearLoadsAndSupplies(state);

            state.GroupedNets ??= GroupByNetworkDepth(state);
            DebugTools.Assert(state.GroupedNets.Select(x => x.Count).Sum() == state.Networks.Count);

            // Each network height layer can be run in parallel without issues.
            var opts = new ParallelOptions { MaxDegreeOfParallelism = parallel };
            for (var i = state.GroupedNets.Count - 1; i >= 0; i--)
            {
                // Note that many net-layers only have a handful of networks.
                // E.g., the number of nets from lowest to heights for box and saltern are:
                // Saltern: 1477, 11, 2, 2, 3.
                // Box:     3308, 20, 1, 5.
                //
                // I have NFI what the overhead for a Parallel.ForEach is, and how it compares to computing differently sized nets.
                //
                // TODO make GroupByNetworkDepth evaluate the TOTAL size of each layer (i.e. loads + chargers + suppliers + discharger)
                // Then decide based on total layer size whether its worth parallelizing that layer?
                Parallel.ForEach(state.GroupedNets[i], opts, net => UpdateNetwork(net, state, frameTime));
            }

            ClearBatteries(state);

            PowerSolverShared.UpdateRampPositions(frameTime, state);
        }

        private void ClearLoadsAndSupplies(PowerState state)
        {
            foreach (var load in state.Loads.Values)
            {
                if (load.Paused)
                    continue;

                load.ReceivingPower = 0;
            }

            foreach (var supply in state.Supplies.Values)
            {
                if (supply.Paused)
                    continue;

                supply.CurrentSupply = 0;
                supply.SupplyRampTarget = 0;
            }
        }

        private void UpdateNetwork(Network network, PowerState state, float frameTime)
        {
            // TODO Look at SIMD.
            // a lot of this is performing very basic math on arrays of data objects like batteries
            // this really shouldn't be hard to do.
            // except for maybe the paused/enabled guff. If its mostly false, I guess they could just be 0 multipliers?

            // Add up demand in network.
            var demand = 0f;
            foreach (var loadId in network.Loads)
            {
                var load = state.Loads[loadId];

                if (!load.Enabled || load.Paused)
                    continue;

                DebugTools.Assert(load.DesiredPower >= 0);
                demand += load.DesiredPower;
            }

            // TODO: Consider having battery charge loads be processed "after" pass-through loads.
            // This would mean that charge rate would have no impact on throughput rate like it does currently.
            // Would require a second pass over the network, or something. Not sure.

            // Loading batteries.
            foreach (var batteryId in network.BatteriesCharging)
            {
                var battery = state.Batteries[batteryId];
                if (!battery.Enabled || !battery.CanCharge || battery.Paused)
                    continue;

                var batterySpace = (battery.Capacity - battery.CurrentStorage) * (1 / battery.Efficiency);
                batterySpace = Math.Max(0, batterySpace);
                var scaledSpace = batterySpace / frameTime;

                var chargeRate = battery.MaxChargeRate + battery.LoadingNetworkDemand / battery.Efficiency;

                var batDemand = Math.Min(chargeRate, scaledSpace);

                DebugTools.Assert(batDemand >= 0);

                battery.DesiredPower = batDemand;
                demand += batDemand;
            }

            DebugTools.Assert(demand >= 0);

            // Add up supply in network.
            var availableSupplySum = 0f;
            var maxSupplySum = 0f;
            foreach (var supplyId in network.Supplies)
            {
                var supply = state.Supplies[supplyId];
                if (!supply.Enabled || supply.Paused)
                    continue;

                var rampMax = supply.SupplyRampPosition + supply.SupplyRampTolerance;
                var effectiveSupply = Math.Min(rampMax, supply.MaxSupply);

                DebugTools.Assert(effectiveSupply >= 0);
                DebugTools.Assert(supply.MaxSupply >= 0);

                supply.EffectiveMaxSupply = effectiveSupply;
                availableSupplySum += effectiveSupply;
                maxSupplySum += supply.MaxSupply;
            }

            var unmet = Math.Max(0, demand - availableSupplySum);

            DebugTools.Assert(availableSupplySum >= 0);
            DebugTools.Assert(maxSupplySum >= 0);

            // Supplying batteries.
            // Batteries need to go after local supplies so that local supplies are prioritized.
            // Also, it makes demand-pulling of batteries
            // Because all batteries will will desire the unmet demand of their loading network,
            // there will be a "rush" of input current when a network powers on,
            // before power stabilizes in the network.
            // This is fine.
            if (unmet > 0)
            {
                foreach (var batteryId in network.BatteriesDischarging)
                {
                    var battery = state.Batteries[batteryId];
                    if (!battery.Enabled || !battery.CanDischarge || battery.Paused)
                        continue;

                    var scaledSpace = battery.CurrentStorage / frameTime;
                    var supplyCap = Math.Min(battery.MaxSupply,
                        battery.SupplyRampPosition + battery.SupplyRampTolerance);
                    var supplyAndPassthrough = supplyCap + battery.CurrentReceiving * battery.Efficiency;
                    var tempSupply = Math.Min(scaledSpace, supplyAndPassthrough);
                    // Clamp final supply to the unmet demand, so that batteries refrain from taking power away from supplies.
                    var clampedSupply = Math.Min(unmet, tempSupply);

                    DebugTools.Assert(clampedSupply >= 0);

                    battery.TempMaxSupply = clampedSupply;
                    availableSupplySum += clampedSupply;
                    // TODO: Calculate this properly.
                    maxSupplySum += clampedSupply;

                    battery.LoadingNetworkDemand = unmet;
                    battery.LoadingDemandMarked = true;
                }
            }
            else
            {
                foreach (var batteryId in network.BatteriesDischarging)
                {
                    var battery = state.Batteries[batteryId];
                    if (!battery.Enabled || !battery.CanDischarge || battery.Paused)
                        continue;

                    battery.TempMaxSupply = 0;
                    battery.LoadingNetworkDemand = 0;
                    battery.LoadingDemandMarked = true;
                }
            }

            network.LastAvailableSupplySum = availableSupplySum;
            network.LastMaxSupplySum = maxSupplySum;

            var met = Math.Min(demand, availableSupplySum);

            if (met == 0)
                return;

            // Distribute supply to loads.
            foreach (var loadId in network.Loads)
            {
                var load = state.Loads[loadId];
                if (!load.Enabled || load.DesiredPower == 0 || load.Paused)
                    continue;

                var ratio = load.DesiredPower / demand;
                load.ReceivingPower = ratio * met;
            }

            // Loading batteries
            foreach (var batteryId in network.BatteriesCharging)
            {
                var battery = state.Batteries[batteryId];

                if (!battery.Enabled || battery.DesiredPower == 0 || battery.Paused)
                    continue;

                var ratio = battery.DesiredPower / demand;
                battery.CurrentReceiving = ratio * met;
                var receivedPower = frameTime * battery.CurrentReceiving;
                receivedPower *= battery.Efficiency;
                battery.CurrentStorage = Math.Min(
                    battery.Capacity,
                    battery.CurrentStorage + receivedPower);
                battery.LoadingMarked = true;
            }

            // Load to supplies
            foreach (var supplyId in network.Supplies)
            {
                var supply = state.Supplies[supplyId];
                if (!supply.Enabled || supply.EffectiveMaxSupply == 0 || supply.Paused)
                    continue;

                var ratio = supply.EffectiveMaxSupply / availableSupplySum;
                supply.CurrentSupply = ratio * met;

                if (supply.MaxSupply != 0)
                {
                    var maxSupplyRatio = supply.MaxSupply / maxSupplySum;

                    supply.SupplyRampTarget = maxSupplyRatio * demand;
                }
                else
                {
                    supply.SupplyRampTarget = 0;
                }
            }

            // Load to supplying batteries
            foreach (var batteryId in network.BatteriesDischarging)
            {
                var battery = state.Batteries[batteryId];
                if (!battery.Enabled || battery.TempMaxSupply == 0 || battery.Paused)
                    continue;

                var ratio = battery.TempMaxSupply / availableSupplySum;
                battery.CurrentSupply = ratio * met;

                battery.CurrentStorage = Math.Max(
                    0,
                    battery.CurrentStorage - frameTime * battery.CurrentSupply);

                battery.SupplyRampTarget = battery.CurrentSupply - battery.CurrentReceiving * battery.Efficiency;

                /*var maxSupplyRatio = supply.MaxSupply / maxSupplySum;

                supply.SupplyRampTarget = maxSupplyRatio * demand;*/
                battery.SupplyingMarked = true;
            }
        }

        private void ClearBatteries(PowerState state)
        {
            // Clear supplying/loading on any batteries that haven't been marked by usage.
            // Because we need this data while processing ramp-pegging, we can't clear it at the start.
            foreach (var battery in state.Batteries.Values)
            {
                if (battery.Paused)
                    continue;

                if (!battery.SupplyingMarked)
                    battery.CurrentSupply = 0;

                if (!battery.LoadingMarked)
                    battery.CurrentReceiving = 0;

                if (!battery.LoadingDemandMarked)
                    battery.LoadingNetworkDemand = 0;

                battery.SupplyingMarked = false;
                battery.LoadingMarked = false;
                battery.LoadingDemandMarked = false;
            }
        }

        private List<List<Network>> GroupByNetworkDepth(PowerState state)
        {
            List<List<Network>> groupedNetworks = new() { new() };
            foreach (var network in state.Networks.Values)
            {
                network.Height = -1;
            }

            foreach (var network in state.Networks.Values)
            {
                if (network.Height == -1)
                    RecursivelyEstimateNetworkDepth(state, network, groupedNetworks);
            }

            return groupedNetworks;
        }

        private static void RecursivelyEstimateNetworkDepth(PowerState state, Network network, List<List<Network>> groupedNetworks)
        {
            network.Height = -2;
            var height = -1;

            foreach (var batteryId in network.BatteriesCharging)
            {
                var battery = state.Batteries[batteryId];

                if (battery.LinkedNetworkDischarging == default || battery.LinkedNetworkDischarging == network.Id)
                    continue;

                var subNet = state.Networks[battery.LinkedNetworkDischarging];
                if (subNet.Height == -1)
                    RecursivelyEstimateNetworkDepth(state, subNet, groupedNetworks);
                else if (subNet.Height == -2)
                {
                    // this network is currently computing its own height (we encountered a loop).
                    continue;
                }

                height = Math.Max(subNet.Height, height);
            }

            network.Height = 1 + height;

            if (network.Height >= groupedNetworks.Count)
                groupedNetworks.Add(new() { network });
            else
                groupedNetworks[network.Height].Add(network);
        }
    }
}
