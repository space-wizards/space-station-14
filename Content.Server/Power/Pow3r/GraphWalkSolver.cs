using System;
using System.Collections.Generic;
using System.Linq;
using static Content.Server.Power.Pow3r.PowerState;

namespace Content.Server.Power.Pow3r
{
    /// <summary>
    ///     Partial implementation of full-graph-walking power solving under pow3r.
    ///     Concept described at https://hackmd.io/@ss14/lowpower
    /// </summary>
    /// <remarks>
    ///     Many features like batteries, cycle detection, join handling, etc... are not implemented at all.
    ///     Seriously, this implementation barely works. Ah well.
    ///     <see cref="BatteryRampPegSolver"/> is better.
    /// </remarks>
    public class GraphWalkSolver : IPowerSolver
    {
        public void Tick(float frameTime, PowerState state)
        {
            foreach (var load in state.Loads.Values)
            {
                load.ReceivingPower = 0;
            }

            foreach (var supply in state.Supplies.Values)
            {
                supply.CurrentSupply = 0;
            }

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

            PowerSolverShared.UpdateRampPositions(frameTime, state);
        }

        private int TotalSubLoadCount(PowerState state, Network network)
        {
            // TODO: Cycle detection.
            var height = network.Loads.Count;

            foreach (var batteryId in network.BatteriesCharging)
            {
                var battery = state.Batteries[batteryId];
                if (battery.LinkedNetworkDischarging != default)
                {
                    height += TotalSubLoadCount(state, state.Networks[battery.LinkedNetworkDischarging]);
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

            foreach (var batteryId in network.BatteriesCharging)
            {
                var battery = state.Batteries[batteryId];
                if (battery.LinkedNetworkDischarging != default)
                {
                    GetLoadingNetworksRecursively(
                        state,
                        state.Networks[battery.LinkedNetworkDischarging],
                        networks,
                        ref totalDemand);
                }
            }
        }
    }
}
