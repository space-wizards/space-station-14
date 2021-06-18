using System.Collections.Generic;
using System.Diagnostics;
using Content.Server.Power.Pow3r;
using static Content.Server.Power.Pow3r.PowerState;


namespace Pow3r
{
    internal sealed partial class Program
    {
        private const int MaxTickData = 180;

        private int _nextId = 1;
        private PowerState _state = new();
        private Network _linking;
        private int _tickDataIdx;
        private bool _paused;

        private readonly string[] _solverNames =
        {
            nameof(GraphWalkSolver),
            nameof(BatteryRampPegSolver),
            nameof(NoOpSolver)
        };

        private readonly IPowerSolver[] _solvers = {
            new GraphWalkSolver(),
            new BatteryRampPegSolver(),
            new NoOpSolver()
        };

        private int _currentSolver;

        private readonly float[] _simTickTimes = new float[MaxTickData];
        private readonly Queue<object> _remQueue = new();
        private readonly Stopwatch _simStopwatch = new Stopwatch();

        private NodeId AllocId()
        {
            return new(_nextId++);
        }

        private void Tick(float frameTime)
        {
            if (_paused)
                return;

            RunSingleStep(frameTime);
        }

        private void RunSingleStep(float frameTime)
        {
            _simStopwatch.Restart();
            _tickDataIdx = (_tickDataIdx + 1) % MaxTickData;

            _solvers[_currentSolver].Tick(frameTime, _state);

            // Update tick history.
            foreach (var load in _state.Loads.Values)
            {
                load.ReceivedPowerData[_tickDataIdx] = load.ReceivingPower;
            }

            foreach (var supply in _state.Supplies.Values)
            {
                supply.SuppliedPowerData[_tickDataIdx] = supply.CurrentSupply;
            }

            foreach (var battery in _state.Batteries.Values)
            {
                battery.StoredPowerData[_tickDataIdx] = battery.CurrentStorage;
                battery.ReceivingPowerData[_tickDataIdx] = battery.CurrentReceiving;
                battery.SuppliedPowerData[_tickDataIdx] = battery.CurrentSupply;
            }

            _simTickTimes[_tickDataIdx] = (float) _simStopwatch.Elapsed.TotalMilliseconds;
        }

        private void RunSingleStep()
        {
            RunSingleStep(1f/_tps);
        }

        // Link data is stored authoritatively on networks,
        // but for easy access it is replicated into the linked components.
        // This is updated here.
        private void RefreshLinks()
        {
            foreach (var battery in _state.Batteries.Values)
            {
                battery.LinkedNetworkLoading = default;
                battery.LinkedNetworkSupplying = default;
            }

            foreach (var load in _state.Loads.Values)
            {
                load.LinkedNetwork = default;
            }

            foreach (var supply in _state.Supplies.Values)
            {
                supply.LinkedNetwork = default;
            }

            foreach (var network in _state.Networks.Values)
            {
                foreach (var loadId in network.Loads)
                {
                    var load = _state.Loads[loadId];
                    load.LinkedNetwork = network.Id;
                }

                foreach (var supplyId in network.Supplies)
                {
                    var supply = _state.Supplies[supplyId];
                    supply.LinkedNetwork = network.Id;
                }

                foreach (var batteryId in network.BatteriesLoading)
                {
                    var battery = _state.Batteries[batteryId];
                    battery.LinkedNetworkLoading = network.Id;
                }

                foreach (var batteryId in network.BatteriesSupplying)
                {
                    var battery = _state.Batteries[batteryId];
                    battery.LinkedNetworkSupplying = network.Id;
                }
            }
        }

    }
}
