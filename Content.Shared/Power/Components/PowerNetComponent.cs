using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

/// <summary>
/// Assigned to a power network node group entity in null-space.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PowerNetComponent : Component
{
    [ViewVariables]
    public readonly HashSet<EntityUid> Chargers = new();

    [ViewVariables]
    public readonly HashSet<EntityUid> Dischargers = new();

    [ViewVariables]
    public HashSet<EntityUid> Consumers { get; set; } = new();

    [ViewVariables]
    public HashSet<EntityUid> Suppliers { get; set; } = new();

    [ViewVariables]
    public readonly HashSet<EntityUid> Apcs = new();

    [ViewVariables]
    public readonly HashSet<EntityUid> Providers = new();
}
