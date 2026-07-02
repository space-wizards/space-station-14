using Content.Shared.Collections;
using Content.Shared.Power.Pow3r;
using Content.Shared.Power.Pow3r.Nodes;

namespace Content.Server.Power.Pow3r;

public sealed class PowerState
{
    public GenIdStorage<IPowerSupply> Supplies = new();
    public GenIdStorage<IPowerNetwork> Networks = new();
    public GenIdStorage<IPowerLoad> Loads = new();
    public GenIdStorage<IPowerBattery> Batteries = new();
    public List<List<IPowerNetwork>>? GroupedNets;
}
