using Content.Shared.Power.Pow3r;
using Robust.Shared.GameStates;

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
    public HashSet<EntityUid> Chargers = new();

    [ViewVariables]
    public HashSet<EntityUid> Dischargers = new();

    [ViewVariables]
    public HashSet<EntityUid> Consumers = new();

    [ViewVariables]
    public HashSet<EntityUid> Suppliers = new();

    [ViewVariables]
    public float LastCombinedLoad => Network.LastCombinedLoad;

    [ViewVariables]
    public float LastCombinedSupply => Network.LastCombinedSupply;

    [ViewVariables]
    public IPowerNetwork Network = new PowerNetwork();
}
