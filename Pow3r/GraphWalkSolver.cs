using System;
using System.Collections.Generic;
using System.Linq;
using static Pow3r.PowerState;

namespace Pow3r
{
    public class GraphWalkSolver : IPowerSolver
    {
        public void Tick(float frameTime, PowerState state, int tickDataIdx)
        {
            state.Loads.Values.ForEach(l => l.ReceivingPower = 0);
            state.Supplies.Values.ForEach(g => g.CurrentSupply = 0);

            foreach (var network in state.Networks.Values)
            {
                // Clear some stuff.
                network.LocalDemandMet = 0;

                // Add up demands in network.
                network.LocalDemandTotal = network.Loads
                    .Select(l => state.Loads[l])
                    .Where(c => c.Enabled)
                    .Sum(c => c.DesiredPower);

                // Add up supplies in network.
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

                network.AvailableSupplyTotal = availableSupplySum;
                network.TheoreticalSupplyTotal = maxSupplySum;
            }

            // Sort networks by tree height so that suppliers that have less possible loads go FIRST.
            // Idea being that a backup generator on a small subnet should do more work
            // so that a larger generator that covers more networks can put its power elsewhere.
            var sortedByHeight = state.Networks.Values.OrderBy(v => TotalSubLoadCount(state, v)).ToArray();

            // Go over every network with supply to send power.
            foreach (var network in sortedByHeight)
            {
                // Find all loads recursively, and sum them up.
                var subNets = new List<Network>();
                var totalDemand = 0f;
                GetLoadingNetworksRecursively(state, network, subNets, ref totalDemand);

                if (totalDemand == 0)
                    continue;

                // Calculate power delivered.
                var power = Math.Min(totalDemand, network.AvailableSupplyTotal);

                // Distribute load across supplies in network.
                foreach (var supplyId in network.Supplies)
                {
                    var supply = state.Supplies[supplyId];
                    if (!supply.Enabled)
                        continue;

                    if (supply.EffectiveMaxSupply != 0)
                    {
                        var ratio = supply.EffectiveMaxSupply / network.AvailableSupplyTotal;

                        supply.CurrentSupply = ratio * power;
                    }
                    else
                    {
                        supply.CurrentSupply = 0;
                    }

                    if (supply.MaxSupply != 0)
                    {
                        var ratio = supply.MaxSupply / network.TheoreticalSupplyTotal;

                        supply.SupplyRampTarget = ratio * totalDemand;
                    }
                    else
                    {
                        supply.SupplyRampTarget = 0;
                    }
                }

                // Distribute supply across subnet loads.
                foreach (var subNet in subNets)
                {
                    var rem = subNet.RemainingDemand;
                    var ratio = rem / totalDemand;

                    subNet.LocalDemandMet += ratio * power;
                }
            }

            // Distribute power across loads in networks.
            foreach (var network in state.Networks.Values)
            {
                if (network.LocalDemandMet == 0)
                    continue;

                foreach (var loadId in network.Loads)
                {
                    var load = state.Loads[loadId];
                    if (!load.Enabled)
                        continue;

                    var ratio = load.DesiredPower / network.LocalDemandTotal;
                    load.ReceivingPower = ratio * network.LocalDemandMet;
                }
            }

            // Update supplies to move their ramp position towards target, if necessary.
            foreach (var supply in state.Supplies.Values)
            {
                if (!supply.Enabled)
                {
                    // If disabled, set ramp to 0.
                    supply.SupplyRampPosition = 0;
                    continue;
                }

                var rampDev = supply.SupplyRampTarget - supply.SupplyRampPosition;
                if (Math.Abs(rampDev) > 0.001f)
                {
                    float newPos;
                    if (rampDev > 0)
                    {
                        // Position below target, go up.
                        newPos = Math.Min(
                            supply.SupplyRampTarget,
                            supply.SupplyRampPosition + supply.SupplyRampRate * frameTime);
                    }
                    else
                    {
                        // Other way around, go down
                        newPos = Math.Max(
                            supply.SupplyRampTarget,
                            supply.SupplyRampPosition - supply.SupplyRampRate * frameTime);
                    }

                    supply.SupplyRampPosition = Math.Clamp(newPos, 0, supply.MaxSupply);
                }
                else
                {
                    supply.SupplyRampPosition = supply.SupplyRampTarget;
                }
            }

            // Update tick history.
            foreach (var load in state.Loads.Values)
            {
                load.ReceivedPowerData[tickDataIdx] = load.ReceivingPower;
            }

            foreach (var supply in state.Supplies.Values)
            {
                supply.SuppliedPowerData[tickDataIdx] = supply.CurrentSupply;
            }
        }

        private int TotalSubLoadCount(PowerState state, Network network)
        {
            // TODO: Cycle detection.
            var height = network.Loads.Count;

            foreach (var batteryId in network.BatteriesLoading)
            {
                var battery = state.Batteries[batteryId];
                if (battery.LinkedNetworkSupplying != default)
                {
                    height += TotalSubLoadCount(state, state.Networks[battery.LinkedNetworkSupplying]);
                }
            }

            return height;
        }

        private void GetLoadingNetworksRecursively(
            PowerState state,
            Network network,
            List<Network> networks,
            ref float totalDemand)
        {
            networks.Add(network);
            totalDemand += network.LocalDemandTotal - network.LocalDemandMet;

            foreach (var batteryId in network.BatteriesLoading)
            {
                var battery = state.Batteries[batteryId];
                if (battery.LinkedNetworkSupplying != default)
                {
                    GetLoadingNetworksRecursively(
                        state,
                        state.Networks[battery.LinkedNetworkSupplying],
                        networks,
                        ref totalDemand);
                }
            }
        }
    }
}
