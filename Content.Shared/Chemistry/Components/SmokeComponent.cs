using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Stores solution on an anchored entity that has touch and ingestion reactions
/// to entities that collide with it. Similar to <see cref="PuddleComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SmokeComponent : Component
{
    public const string SolutionName = "solutionArea";

    /// <summary>
    /// The solution on the entity with touch and ingestion reactions.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? Solution = null;

    /// <summary>
    /// The max amount of tiles this smoke cloud can spread to.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int SpreadAmount;

    /// <summary>
    /// The max rate at which chemicals are transferred from the smoke to the person inhaling it.
    /// Calculated as (total volume of chemicals in smoke) / (<see cref="Duration"/>)
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 TransferRate;

    /// <summary>
    /// The total lifespan of the smoke.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Duration = 10;
}
