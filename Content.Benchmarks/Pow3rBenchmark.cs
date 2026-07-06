using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Content.Server.Power.Pow3r;
using Content.Server.Power.Pow3r.Solvers;
using Content.Shared.Collections;
using Content.Shared.Power.Pow3r;
using Content.Shared.Power.Pow3r.Nodes;
using Robust.Shared.Analyzers;
using Robust.Shared.Threading;
using Robust.UnitTesting;

namespace Content.Benchmarks;

[Virtual]
[SimpleJob]
public class Pow3rBenchmark
{
    private BatteryRampPegSolver _solver = new();
    private PowerState _state = new();
    private IParallelManager _parallel = new TestingParallelManager();
    private PowerNetwork _chargeNetwork = new();
    private PowerNetwork _dischargeNetwork = new();
    private List<NodeId> _loads = new();

    public int SupplyCount { get; set; } = 100;
    public int LoadCount { get; set; } = 2000;
    public int BatteryCount { get; set; } = 100;

    public int TicksAmount { get; set; } = 60000;

    [Params(true, false)] public bool ChargeDischarge { get; set; }

    public float TickRate = 1f / 30f;

    [GlobalSetup]
    public void Setup()
    {
        _state.Networks.Allocate(out var chargeNetId) = _chargeNetwork;
        _state.Networks.Allocate(out var dischargeNetId) = _dischargeNetwork;

        for (int i = 0; i < SupplyCount; i++)
        {
            var supply = new PowerSupply();
            _state.Supplies.Allocate(out var supplyId) = supply;
            supply.Id = supplyId;
            _dischargeNetwork.Supplies.Add(supplyId);
            supply.AvailableSupply = 5000;
            supply.LinkedNetwork = dischargeNetId;
        }

        for (int i = 0; i < LoadCount; i++)
        {
            var load = new PowerLoad();
            _state.Loads.Allocate(out var loadId) = load;
            load.Id = loadId;
            _chargeNetwork.Loads.Add(load.Id);
            load.DesiredPower = ChargeDischarge ? 0 : 500;
            load.LinkedNetwork = chargeNetId;
            _loads.Add(loadId);
        }

        for (int i = 0; i < BatteryCount; i++)
        {
            var battery = new PowerBattery();
            _state.Batteries.Allocate(out var batteryId) = battery;
            battery.Id = batteryId;
            battery.Capacity = 500000;
            _chargeNetwork.BatteryLoads.Add(batteryId);
            _dischargeNetwork.BatteryLoads.Add(batteryId);
            battery.LinkedNetworkCharging = chargeNetId;
            battery.LinkedNetworkDischarging = dischargeNetId;
        }
    }

    [Benchmark(Description = "Run Pow3r Idle")]
    public void RunPowerIdle()
    {
        var ticks = ChargeDischarge ? TicksAmount / 2 : TicksAmount;
        for (int i = 0; i < ticks; i++)
        {
            _solver.Tick(TickRate, _state, _parallel);
        }

        if (!ChargeDischarge)
            return;

        for (int i = 0; i < LoadCount; i++)
        {
            _state.Loads[_loads[i]].DesiredPower = 1000;
        }

        for (int i = 0; i < ticks; i++)
        {
            _solver.Tick(TickRate, _state, _parallel);
        }
    }
}
