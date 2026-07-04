using Content.Shared.Power.Pow3r;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Power.Components;

/// <summary>
/// Assigned to a power network node group entity in null-space.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PowerNetComponent : Component
{
    [DataField]
    public Voltage Voltage;

    [ViewVariables]
    public readonly HashSet<EntityUid> Apcs = new();

    [ViewVariables]
    public readonly HashSet<EntityUid> Providers = new();

    [ViewVariables]
    public HashSet<EntityUid> Chargers
    {
        get => Network.Chargers;
        set => Network.Chargers = value;
    }

    [ViewVariables]
    public HashSet<EntityUid> Dischargers
    {
        get => Network.Dischargers;
        set => Network.Dischargers = value;
    }

    [ViewVariables]
    public HashSet<EntityUid> Consumers
    {
        get => Network.Consumers;
        set => Network.Consumers = value;
    }

    [ViewVariables]
    public HashSet<EntityUid> Suppliers
    {
        get => Network.Suppliers;
        set => Network.Suppliers = value;
    }

    [ViewVariables]
    public float LastCombinedLoad => Network.LastCombinedLoad;

    [ViewVariables]
    public float LastCombinedSupply => Network.LastCombinedSupply;

    [ViewVariables]
    public IPowerNetwork Network = default!;
}
