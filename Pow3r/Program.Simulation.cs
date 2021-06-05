using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;

namespace Pow3r
{
    internal sealed partial class Program
    {
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            IncludeFields = true,
        };

        private const int MaxTickData = 180;

        private int _nextId;
        private readonly List<Supply> _supplies = new();
        private readonly List<Network> _networks = new();
        private readonly List<Load> _loads = new();
        private readonly List<Battery> _batteries = new();
        private bool _showDemo;
        private Network _linking;
        private int _tickDataIdx;
        private bool _paused;

        private readonly float[] _simTickTimes = new float[MaxTickData];
        private readonly Queue<object> _remQueue = new();
        private readonly Stopwatch _simStopwatch = new Stopwatch();

        private void Tick(float frameTime)
        {
            if (_paused)
                return;

            _simStopwatch.Restart();
            _tickDataIdx = (_tickDataIdx + 1) % MaxTickData;

            _loads.ForEach(l => l.ReceivingPower = 0);
            _supplies.ForEach(g => g.CurrentSupply = 0);

            foreach (var network in _networks)
            {
                // Clear some stuff.
                network.MetDemand = 0;

                // Add up demands in network.
                network.DemandTotal = network.Loads
                    .Where(c => c.Enabled)
                    .Sum(c => c.DesiredPower);

                // Add up supplies in network.
                var availableSupplySum = 0f;
                var maxSupplySum = 0f;
                foreach (var supply in network.Supplies)
                {
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
            var sortedByHeight = _networks.OrderBy(TotalSubLoadCount).ToList();

            // Go over every network with supply to send power.
            foreach (var network in sortedByHeight)
            {
                // Find all loads recursively, and sum them up.
                var subNets = new List<Network>();
                var totalDemand = 0f;
                GetLoadingNetworksRecursively(network, subNets, ref totalDemand);

                if (totalDemand == 0)
                    continue;

                // Calculate power delivered.
                var power = Math.Min(totalDemand, network.AvailableSupplyTotal);

                // Distribute load across supplies in network.
                foreach (var supply in network.Supplies)
                {
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

                    subNet.MetDemand += ratio * power;
                }
            }

            // Distribute power across loads in networks.
            foreach (var network in _networks)
            {
                if (network.MetDemand == 0)
                    continue;

                foreach (var load in network.Loads)
                {
                    if (!load.Enabled)
                        continue;

                    var ratio = load.DesiredPower / network.DemandTotal;
                    load.ReceivingPower = ratio * network.MetDemand;
                }
            }

            // Update supplies to move their ramp position towards target, if necessary.
            foreach (var supply in _supplies)
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
            foreach (var load in _loads)
            {
                load.ReceivedPowerData[_tickDataIdx] = load.ReceivingPower;
            }

            foreach (var supply in _supplies)
            {
                supply.SuppliedPowerData[_tickDataIdx] = supply.CurrentSupply;
            }

            _simTickTimes[_tickDataIdx] = (float) _simStopwatch.Elapsed.TotalMilliseconds;
        }

        private static int TotalSubLoadCount(Network network)
        {
            // TODO: Cycle detection.
            var height = network.Loads.Count;

            foreach (var battery in network.BatteriesLoading)
            {
                if (battery.LinkedNetworkSupplying != null)
                {
                    height += TotalSubLoadCount(battery.LinkedNetworkSupplying);
                }
            }

            return height;
        }

        private static void GetLoadingNetworksRecursively(Network network, List<Network> networks,
            ref float totalDemand)
        {
            networks.Add(network);
            totalDemand += network.DemandTotal - network.MetDemand;

            foreach (var battery in network.BatteriesLoading)
            {
                if (battery.LinkedNetworkSupplying != null)
                {
                    GetLoadingNetworksRecursively(battery.LinkedNetworkSupplying, networks, ref totalDemand);
                }
            }
        }

        // Link data is stored authoritatively on networks,
        // but for easy access it is replicated into the linked components.
        // This is updated here.
        private void RefreshLinks()
        {
            foreach (var network in _networks)
            {
                foreach (var load in network.Loads)
                {
                    load.LinkedNetwork = network;
                }

                foreach (var supply in network.Supplies)
                {
                    supply.LinkedNetwork = network;
                }

                foreach (var battery in network.BatteriesLoading)
                {
                    battery.LinkedNetworkLoading = network;
                }

                foreach (var battery in network.BatteriesSupplying)
                {
                    battery.LinkedNetworkSupplying = network;
                }
            }
        }

    }
}
