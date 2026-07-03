using Content.Server.Collections;
using Content.Server.Power.Pow3r.Nodes;

namespace Content.Server.Power.Pow3r;

public sealed class PowerState
{
    public GenIdStorage<SolverPowerSupply> Supplies = new();
    public GenIdStorage<SolverPowerNetwork> Networks = new();
    public GenIdStorage<SolverPowerLoad> Loads = new();
    public GenIdStorage<SolverPowerBattery> Batteries = new();
    public List<List<SolverPowerNetwork>>? GroupedNets;
}
