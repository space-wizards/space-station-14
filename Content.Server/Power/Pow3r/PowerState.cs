using Content.Server.Collections;
using Content.Shared.Power.Pow3r;
using Content.Shared.Power.Pow3r.Nodes;

namespace Content.Server.Power.Pow3r;

public sealed class PowerState
{
    public GenIdStorage<PowerSupply> Supplies = new();
    public GenIdStorage<PowerNetwork> Networks = new();
    public GenIdStorage<PowerLoad> Loads = new();
    public GenIdStorage<PowerBattery> Batteries = new();
    public List<List<PowerNetwork>>? GroupedNets;
}
