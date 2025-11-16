using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Allows to sabotage an entity by injecting a certain reagent.
/// Makes it explode when activated or used.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RiggableComponent : Component
{
    /// <summary>
    /// Is this entity currently rigged?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsRigged;

    /// <summary>
    /// Did this entity already explode from being rigged?
    /// Used to prevent it from exploding multiple times from different events
    /// happening in the same tick.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Exploded;

    /// <summary>
    /// The solution that needs to be injected into to rig this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Solution = "battery";

    /// <summary>
    /// The reagent and amount needed for rigging.
    /// </summary>
    [DataField("reagent"), AutoNetworkedField]
    public ReagentQuantity RequiredQuantity = new("Plasma", FixedPoint2.New(5), null);
}
