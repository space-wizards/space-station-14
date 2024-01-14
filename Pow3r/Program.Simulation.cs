using System.Collections.Generic;
using System.Diagnostics;
using Content.Server.Power.Pow3r;
using Robust.Shared.Threading;
using Robust.UnitTesting;
using static Content.Server.Power.Pow3r.PowerState;


namespace Pow3r
{
    internal sealed partial class Program
    {
        private const int MaxTickData = 180;

        private PowerState _state = new();
        private Network _linking;
        private int _tickDataIdx;
        private bool _paused;

        private readonly string[] _solverNames =
        {
            nameof(BatteryRampPegSolver),
            nameof(NoOpSolver)
        };

        private readonly IPowerSolver[] _solvers = {
            new BatteryRampPegSolver(),
            new NoOpSolver()
        };

        private int _currentSolver;

        private readonly float[] _simTickTimes = new float[MaxTickData];
        private readonly Queue<object> _remQueue = new();
        private readonly Stopwatch _simStopwatch = new Stopwatch();

        private IParallelManager _parallel = new TestingParallelManager();

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

            _solvers[_currentSolver].Tick(frameTime, _state, _parallel);

            // Update tick history.
            foreach (var load in _state.Loads.Values)
            {
                var displayLoad = _displayLoads[load.Id];
                displayLoad.ReceivedPowerData[_tickDataIdx] = load.ReceivingPower;
            }

            foreach (var supply in _state.Supplies.Values)
            {
                var displaySupply = _displaySupplies[supply.Id];
                displaySupply.SuppliedPowerData[_tickDataIdx] = supply.CurrentSupply;
            }

            foreach (var battery in _state.Batteries.Values)
            {
                var displayBattery = _displayBatteries[battery.Id];
                displayBattery.StoredPowerData[_tickDataIdx] = battery.CurrentStorage;
                displayBattery.ReceivingPowerData[_tickDataIdx] = battery.CurrentReceiving;
                displayBattery.SuppliedPowerData[_tickDataIdx] = battery.CurrentSupply;
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
                battery.LinkedNetworkCharging = default;
                battery.LinkedNetworkDischarging = default;
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

                foreach (var batteryId in network.BatteryLoads)
                {
                    var battery = _state.Batteries[batteryId];
                    battery.LinkedNetworkCharging = network.Id;
                }

                foreach (var batteryId in network.BatterySupplies)
                {
                    var battery = _state.Batteries[batteryId];
                    battery.LinkedNetworkDischarging = network.Id;
                }
            }
        }

    }
}
