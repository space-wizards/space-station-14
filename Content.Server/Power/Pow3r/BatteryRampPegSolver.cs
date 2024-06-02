using System.Diagnostics;
using Robust.Shared.Utility;
using System.Linq;
using Robust.Shared.Threading;
using static Content.Server.Power.Pow3r.PowerState;

namespace Content.Server.Power.Pow3r
{
    public sealed class BatteryRampPegSolver : IPowerSolver
    {
        private UpdateNetworkJob _networkJob;

        public BatteryRampPegSolver()
        {
            _networkJob = new()
            {
                Solver = this,
            };
        }

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

        public void Tick(float frameTime, PowerState state, IParallelManager parallel)
        {
            ClearLoadsAndSupplies(state);

            state.GroupedNets ??= GroupByNetworkDepth(state);
            DebugTools.Assert(state.GroupedNets.Select(x => x.Count).Sum() == state.Networks.Count);
            _networkJob.State = state;
            _networkJob.FrameTime = frameTime;
            ValidateNetworkGroups(state, state.GroupedNets);

            // Each network height layer can be run in parallel without issues.
            foreach (var group in state.GroupedNets)
            {
                // Note that many net-layers only have a handful of networks.
                // E.g., the number of nets from lowest to highest for box and saltern are:
                // Saltern: 1477, 11, 2, 2, 3.
                // Box:     3308, 20, 1, 5.
                //
                // I have NFI what the overhead for a Parallel.ForEach is, and how it compares to computing differently
                // sized nets. Basic benchmarking shows that this is better, but maybe the highest-tier nets should just
                // be run sequentially? But then again, maybe they are 2-3 very BIG networks at the top? So maybe:
                //
                // TODO make GroupByNetworkDepth evaluate the TOTAL size of each layer (i.e. loads + chargers +
                // suppliers + discharger) Then decide based on total layer size whether its worth parallelizing that
                // layer?
                _networkJob.Networks = group;
                parallel.ProcessNow(_networkJob, group.Count);
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

            // Add up demand from loads.
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

            // Add demand from batteries
            foreach (var batteryId in network.BatteryLoads)
            {
                var battery = state.Batteries[batteryId];
                if (!battery.Enabled || !battery.CanCharge || battery.Paused)
                    continue;

                var batterySpace = (battery.Capacity - battery.CurrentStorage) * (1 / battery.Efficiency);
                batterySpace = Math.Max(0, batterySpace);
                var scaledSpace = batterySpace / frameTime;

                var chargeRate = battery.MaxChargeRate + battery.LoadingNetworkDemand / battery.Efficiency;

                battery.DesiredPower = Math.Min(chargeRate, scaledSpace);
                DebugTools.Assert(battery.DesiredPower >= 0);
                demand += battery.DesiredPower;
            }

            DebugTools.Assert(demand >= 0);

            // Add up supply in network.
            var totalSupply = 0f;
            var totalMaxSupply = 0f;
            foreach (var supplyId in network.Supplies)
            {
                var supply = state.Supplies[supplyId];
                if (!supply.Enabled || supply.Paused)
                    continue;

                var rampMax = supply.SupplyRampPosition + supply.SupplyRampTolerance;
                var effectiveSupply = Math.Min(rampMax, supply.MaxSupply);

                DebugTools.Assert(effectiveSupply >= 0);
                DebugTools.Assert(supply.MaxSupply >= 0);

                supply.AvailableSupply = effectiveSupply;
                totalSupply += effectiveSupply;
                totalMaxSupply += supply.MaxSupply;
            }

            var unmet = Math.Max(0, demand - totalSupply);
            DebugTools.Assert(totalSupply >= 0);
            DebugTools.Assert(totalMaxSupply >= 0);

            // Supplying batteries. Batteries need to go after local supplies so that local supplies are prioritized.
            // Also, it makes demand-pulling of batteries. Because all batteries will desire the unmet demand of their
            // loading network, there will be a "rush" of input current when a network powers on, before power
            // stabilizes in the network. This is fine.

            var totalBatterySupply = 0f;
            var totalMaxBatterySupply = 0f;
            if (unmet > 0)
            {
                // determine supply available from batteries
                foreach (var batteryId in network.BatterySupplies)
                {
                    var battery = state.Batteries[batteryId];
                    if (!battery.Enabled || !battery.CanDischarge || battery.Paused)
                        continue;

                    var scaledSpace = battery.CurrentStorage / frameTime;
                    var supplyCap = Math.Min(battery.MaxSupply,
                        battery.SupplyRampPosition + battery.SupplyRampTolerance);
                    var supplyAndPassthrough = supplyCap + battery.CurrentReceiving * battery.Efficiency;

                    battery.AvailableSupply = Math.Min(scaledSpace, supplyAndPassthrough);
                    battery.LoadingNetworkDemand = unmet;

                    battery.MaxEffectiveSupply = Math.Min(battery.CurrentStorage / frameTime, battery.MaxSupply + battery.CurrentReceiving * battery.Efficiency);
                    totalBatterySupply += battery.AvailableSupply;
                    totalMaxBatterySupply += battery.MaxEffectiveSupply;
                }
            }

            network.LastCombinedLoad = demand;
            network.LastCombinedSupply = totalSupply + totalBatterySupply;
            network.LastCombinedMaxSupply = totalMaxSupply + totalMaxBatterySupply;

            var met = Math.Min(demand, network.LastCombinedSupply);
            if (met == 0)
                return;

            var supplyRatio = met / demand;
            // if supply ratio == 1 (or is close to) we could skip some math for each load & battery.

            // Distribute supply to loads.
            foreach (var loadId in network.Loads)
            {
                var load = state.Loads[loadId];
                if (!load.Enabled || load.DesiredPower == 0 || load.Paused)
                    continue;

                load.ReceivingPower = load.DesiredPower * supplyRatio;
            }

            // Distribute supply to batteries
            foreach (var batteryId in network.BatteryLoads)
            {
                var battery = state.Batteries[batteryId];
                if (!battery.Enabled || battery.DesiredPower == 0 || battery.Paused || !battery.CanCharge)
                    continue;

                battery.LoadingMarked = true;
                battery.CurrentReceiving = battery.DesiredPower * supplyRatio;
                battery.CurrentStorage += frameTime * battery.CurrentReceiving * battery.Efficiency;

                DebugTools.Assert(battery.CurrentStorage <= battery.Capacity || MathHelper.CloseTo(battery.CurrentStorage, battery.Capacity, 1e-5));
                battery.CurrentStorage = MathF.Min(battery.CurrentStorage, battery.Capacity);
            }

            // Target output capacity for supplies
            var metSupply = Math.Min(demand, totalSupply);
            if (metSupply > 0)
            {
                var relativeSupplyOutput = metSupply / totalSupply;
                var targetRelativeSupplyOutput = Math.Min(demand, totalMaxSupply) / totalMaxSupply;

                // Apply load to supplies
                foreach (var supplyId in network.Supplies)
                {
                    var supply = state.Supplies[supplyId];
                    if (!supply.Enabled || supply.Paused)
                        continue;

                    supply.CurrentSupply = supply.AvailableSupply * relativeSupplyOutput;

                    // Supply ramp assumes all supplies ramp at the same rate. If some generators spin up very slowly, in
                    // principle the fast supplies should try over-shoot until they can settle back down. E.g., all supplies
                    // need to reach 50% capacity, but it takes the nuclear reactor 1 hour to reach that, then our lil coal
                    // furnaces should run at 100% for a while. But I guess this is good enough for now.
                    supply.SupplyRampTarget = supply.MaxSupply * targetRelativeSupplyOutput;
                }
            }

            // Return if normal supplies met all demand or there are no supplying batteries
            if (unmet <= 0 || totalMaxBatterySupply <= 0)
                return;

            // Target output capacity for batteries
            var relativeBatteryOutput = Math.Min(unmet, totalBatterySupply) / totalBatterySupply;
            var relativeTargetBatteryOutput = Math.Min(unmet, totalMaxBatterySupply) / totalMaxBatterySupply;

            // Apply load to supplying batteries
            foreach (var batteryId in network.BatterySupplies)
            {
                var battery = state.Batteries[batteryId];
                if (!battery.Enabled || battery.Paused || !battery.CanDischarge)
                    continue;

                battery.SupplyingMarked = true;
                battery.CurrentSupply = battery.AvailableSupply * relativeBatteryOutput;
                // Note that because available supply is always greater than or equal to the current ramp target, if you
                // have multiple batteries running at less than 100% output, then batteries with greater ramp tolerances
                // will contribute a larger relative fraction of output power. This is because while they will both ramp
                // to the same relative maximum output, the larger tolerance will mean that one will have a larger
                // available supply. IMO this is undesirable, but I can't think of an easy fix ATM.

                battery.CurrentStorage -= frameTime * battery.CurrentSupply;
#if DEBUG
                // Manual "MathHelper.CloseToPercent" using the subtracted value to define the relative error.
                if (battery.CurrentStorage < 0)
                {
                    float epsilon = Math.Max(frameTime * battery.CurrentSupply, 1) * 1e-4f;
                    DebugTools.Assert(battery.CurrentStorage > -epsilon);
                }
#endif
                battery.CurrentStorage = MathF.Max(0, battery.CurrentStorage);

                battery.SupplyRampTarget = battery.MaxEffectiveSupply * relativeTargetBatteryOutput - battery.CurrentReceiving * battery.Efficiency;

                DebugTools.Assert(battery.MaxEffectiveSupply * relativeTargetBatteryOutput <= battery.LoadingNetworkDemand
                                  || MathHelper.CloseToPercent(battery.MaxEffectiveSupply * relativeTargetBatteryOutput, battery.LoadingNetworkDemand, 0.001));
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
                {
                    battery.CurrentSupply = 0;
                    battery.SupplyRampTarget = 0;
                    battery.LoadingNetworkDemand = 0;
                }

                if (!battery.LoadingMarked)
                {
                    battery.CurrentReceiving = 0;
                }

                battery.SupplyingMarked = false;
                battery.LoadingMarked = false;
            }
        }

        private List<List<Network>> GroupByNetworkDepth(PowerState state)
        {
            List<List<Network>> groupedNetworks = new();
            foreach (var network in state.Networks.Values)
            {
                network.Height = -1;
            }

            foreach (var network in state.Networks.Values)
            {
                if (network.Height == -1)
                    RecursivelyEstimateNetworkDepth(state, network, groupedNetworks);
            }

            ValidateNetworkGroups(state, groupedNetworks);
            return groupedNetworks;
        }

        /// <summary>
        /// Validate that network grouping is up to date. I.e., that it is safe to solve each networking in a given
        /// group in parallel. This assumes that batteries are the only device that connects to multiple networks, and
        /// is thus the only obstacle to solving everything in parallel.
        /// </summary>
        [Conditional("DEBUG")]
        private void ValidateNetworkGroups(PowerState state, List<List<Network>> groupedNetworks)
        {
            HashSet<Network> nets = new();
            HashSet<NodeId> netIds = new();
            foreach (var layer in groupedNetworks)
            {
                nets.Clear();
                netIds.Clear();

                foreach (var net in layer)
                {
                    foreach (var batteryId in net.BatteryLoads)
                    {
                        var battery = state.Batteries[batteryId];
                        if (battery.LinkedNetworkDischarging == default)
                            continue;

                        var subNet = state.Networks[battery.LinkedNetworkDischarging];
                        if (battery.LinkedNetworkDischarging == net.Id)
                        {
                            DebugTools.Assert(subNet == net);
                            continue;
                        }

                        DebugTools.Assert(!nets.Contains(subNet));
                        DebugTools.Assert(!netIds.Contains(subNet.Id));
                        DebugTools.Assert(subNet.Height < net.Height);
                    }

                    foreach (var batteryId in net.BatterySupplies)
                    {
                        var battery = state.Batteries[batteryId];
                        if (battery.LinkedNetworkCharging == default)
                            continue;

                        var parentNet = state.Networks[battery.LinkedNetworkCharging];
                        if (battery.LinkedNetworkCharging == net.Id)
                        {
                            DebugTools.Assert(parentNet == net);
                            continue;
                        }

                        DebugTools.Assert(!nets.Contains(parentNet));
                        DebugTools.Assert(!netIds.Contains(parentNet.Id));
                        DebugTools.Assert(parentNet.Height > net.Height);
                    }

                    DebugTools.Assert(nets.Add(net));
                    DebugTools.Assert(netIds.Add(net.Id));
                }
            }
        }

        private static void RecursivelyEstimateNetworkDepth(PowerState state, Network network, List<List<Network>> groupedNetworks)
        {
            network.Height = -2;
            var height = -1;

            foreach (var batteryId in network.BatteryLoads)
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

        #region Jobs

        private record struct UpdateNetworkJob : IParallelRobustJob
        {
            public int BatchSize => 4;

            public BatteryRampPegSolver Solver;
            public PowerState State;
            public float FrameTime;
            public List<Network> Networks;

            public void Execute(int index)
            {
                Solver.UpdateNetwork(Networks[index], State, FrameTime);
            }
        }

        #endregion
    }
}
