using System;
using System.Collections.Generic;
using Robust.Shared.Utility;
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

        private Network[] _sortBuffer = Array.Empty<Network>();

        public void Tick(float frameTime, PowerState state)
        {
            // Clear loads and supplies.
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
                if (network.BatteriesDischarging.Count != 0)
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

                network.LastAvailableSupplySum = availableSupplySum;
                network.LastMaxSupplySum = maxSupplySum;

                var met = Math.Min(demand, availableSupplySum);

                if (met != 0)
                {
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

                    // Supplying batteries
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
            }

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

            PowerSolverShared.UpdateRampPositions(frameTime, state);
        }

        private static void EstimateNetworkDepth(PowerState state, Network network)
        {
            network.HeightTouched = true;

            if (network.BatteriesCharging.Count == 0)
            {
                network.Height = 1;
                return;
            }

            var max = 0;
            foreach (var batteryId in network.BatteriesCharging)
            {
                var battery = state.Batteries[batteryId];

                if (battery.LinkedNetworkDischarging == default)
                    continue;

                var subNet = state.Networks[battery.LinkedNetworkDischarging];
                if (!subNet.HeightTouched)
                    EstimateNetworkDepth(state, subNet);

                max = Math.Max(subNet.Height, max);
            }

            network.Height = 1 + max;
        }
    }
}
