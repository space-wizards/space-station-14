namespace Content.Shared.Power.Pow3r;

public struct PowerNetwork : IPowerNetwork
{
    public PowerNetwork()
    {
    }

    [ViewVariables]
    public HashSet<EntityUid> Chargers { get; set; } = new();

    [ViewVariables]
    public HashSet<EntityUid> Dischargers { get; set; } = new();

    [ViewVariables]
    public HashSet<EntityUid> Consumers { get; set; } = new();

    [ViewVariables]
    public HashSet<EntityUid> Suppliers { get; set; } = new();

    [ViewVariables]
    public float LastCombinedLoad { get; set; } = 0;

    [ViewVariables]
    public float LastCombinedSupply { get; set; } = 0;

    [ViewVariables]
    public float LastCombinedMaxSupply { get; set; } = 0;

    [ViewVariables]
    public int Height { get; set; } = 0;
}
