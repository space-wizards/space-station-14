using System;
using System.Collections.Generic;
using static Pow3r.PowerState;

namespace Pow3r
{
    public sealed class BatteryRampPegSolver : IPowerSolver
    {
        private sealed class HeightComparer : IComparer<Network>
        {
            public static HeightComparer Instance { get; } = new();

            public int Compare(Network x, Network y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (ReferenceEquals(null, y)) return 1;
                if (ReferenceEquals(null, x)) return -1;
                return x.Height.CompareTo(y.Height);
            }
        }

        private Network[] _sortBuffer = new Network[0];

        public void Tick(float frameTime, PowerState state)
        {
            // Clear loads and supplies.
            state.Loads.Values.ForEach(l => l.ReceivingPower = 0);
            state.Supplies.Values.ForEach(g => g.CurrentSupply = 0);
            state.Supplies.Values.ForEach(g => g.SupplyRampTarget = 0);

            // Run a pass to estimate network tree graph height.
            // This is so that we can run networks before their children,
            // to avoid draining batteries for a tick if their passing-supply gets cut off.
            // It's not a big loss if this doesn't work (it won't, in some scenarios), but it's a nice-to-have.
            foreach (var network in state.Networks.Values)
            {
                network.HeightTouched = false;
                network.Height = -1;
            }

            foreach (var network in state.Networks.Values)
            {
                if (network.BatteriesSupplying.Count != 0)
                    continue;

                EstimateNetworkDepth(state, network);
            }

            if (_sortBuffer.Length != state.Networks.Count)
                _sortBuffer = new Network[state.Networks.Count];

            var i = 0;
            foreach (var network in state.Networks.Values)
            {
                _sortBuffer[i++] = network;
            }

            Array.Sort(_sortBuffer, HeightComparer.Instance);

            // Go over every network.
            foreach (var network in _sortBuffer)
            {
                // Add up demand in network.
                var demand = 0f;
                foreach (var loadId in network.Loads)
                {
                    var load = state.Loads[loadId];

                    if (!load.Enabled)
                        continue;

                    demand += load.DesiredPower;
                }

                // Loading batteries.
                foreach (var batteryId in network.BatteriesLoading)
                {
                    var battery = state.Batteries[batteryId];
                    if (!battery.Enabled)
                        continue;

                    var batterySpace = battery.Capacity - battery.CurrentStorage;
                    var scaledSpace = batterySpace / frameTime;

                    var chargeRate = battery.MaxChargeRate + battery.LoadingNetworkDemand;

                    var batDemand = Math.Min(chargeRate, scaledSpace);
                    battery.DesiredPower = batDemand;
                    demand += batDemand;
                }

                // Add up supply in network.
                var availableSupplySum = 0f;
                var maxSupplySum = 0f;
                foreach (var supplyId in network.Supplies)
                {
                    var supply = state.Supplies[supplyId];
                    if (!supply.Enabled)
                        continue;

                    var rampMax = supply.SupplyRampPosition + supply.SupplyRampTolerance;
                    var effectiveSupply = Math.Min(rampMax, supply.MaxSupply);
                    supply.EffectiveMaxSupply = effectiveSupply;
                    availableSupplySum += effectiveSupply;
                    maxSupplySum += supply.MaxSupply;
                }

                var unmet = demand - availableSupplySum;

                // Supplying batteries.
                // Batteries need to go after local supplies so that local supplies are prioritized.
                // Also, it makes demand-pulling of batteries
                // Because all batteries will will desire the unmet demand of their loading network,
                // there will be a "rush" of input current when a network powers on,
                // before power stabilizes in the network.
                // This is fine.
                foreach (var batteryId in network.BatteriesSupplying)
                {
                    var battery = state.Batteries[batteryId];
                    if (!battery.Enabled)
                        continue;

                    var scaledSpace = battery.CurrentStorage / frameTime;
                    var supplyCap = Math.Min(battery.MaxSupply,
                        battery.SupplyRampPosition + battery.SupplyRampTolerance);
                    var supplyAndPassthrough = supplyCap + battery.CurrentReceiving;
                    var tempSupply = Math.Min(scaledSpace, supplyAndPassthrough);

                    battery.TempMaxSupply = tempSupply;
                    availableSupplySum += tempSupply;
                    // TODO: Calculate this properly.
                    maxSupplySum += tempSupply;
                    battery.LoadingNetworkDemand = unmet;
                    battery.LoadingDemandMarked = true;
                }

                var met = Math.Min(demand, availableSupplySum);

                if (met != 0)
                {
                    // Distribute supply to loads.
                    foreach (var loadId in network.Loads)
                    {
                        var load = state.Loads[loadId];
                        if (!load.Enabled)
                            continue;

                        var ratio = load.DesiredPower / demand;
                        load.ReceivingPower = ratio * met;
                    }

                    // Loading batteries
                    foreach (var batteryId in network.BatteriesLoading)
                    {
                        var battery = state.Batteries[batteryId];

                        if (!battery.Enabled)
                            continue;

                        var ratio = battery.DesiredPower / demand;
                        battery.CurrentReceiving = ratio * met;
                        battery.CurrentStorage += frameTime * battery.CurrentReceiving;
                        battery.LoadingMarked = true;
                    }

                    // Load to supplies
                    foreach (var supplyId in network.Supplies)
                    {
                        var supply = state.Supplies[supplyId];
                        if (!supply.Enabled || supply.EffectiveMaxSupply == 0)
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

                    // Supplying batteries
                    foreach (var batteryId in network.BatteriesSupplying)
                    {
                        var battery = state.Batteries[batteryId];
                        if (!battery.Enabled || battery.TempMaxSupply == 0)
                            continue;

                        var ratio = battery.TempMaxSupply / availableSupplySum;
                        battery.CurrentSupply = ratio * met;
                        battery.CurrentStorage -= frameTime * battery.CurrentSupply;

                        battery.SupplyRampTarget = battery.CurrentSupply - battery.CurrentReceiving;

                        /*var maxSupplyRatio = supply.MaxSupply / maxSupplySum;

                        supply.SupplyRampTarget = maxSupplyRatio * demand;*/
                        battery.SupplyingMarked = true;
                    }
                }
            }

            // Clear supplying/loading on any batteries that haven't been marked by usage.
            // Because we need this data while processing ramp-pegging, we can't clear it at the start.
            foreach (var battery in state.Batteries.Values)
            {
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

            PowerSolverShared.UpdateSupplyRampPositions(frameTime, state);
        }

        private static void EstimateNetworkDepth(PowerState state, Network network)
        {
            network.HeightTouched = true;

            if (network.BatteriesLoading.Count == 0)
            {
                network.Height = 1;
                return;
            }

            var max = 0;
            foreach (var batteryId in network.BatteriesLoading)
            {
                var battery = state.Batteries[batteryId];

                if (battery.LinkedNetworkSupplying == default)
                    continue;

                var subNet = state.Networks[battery.LinkedNetworkSupplying];
                if (!subNet.HeightTouched)
                    EstimateNetworkDepth(state, subNet);

                max = Math.Max(subNet.Height, max);
            }

            network.Height = 1 + max;
        }
    }
}
