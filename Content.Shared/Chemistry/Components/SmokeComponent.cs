using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Spawns and spreads entities that can contain and deliver reagents
/// to entities that collide with it. Similar to <see cref="PuddleComponent"/>
/// <seealso cref="SmokeSourceComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SmokeComponent : Component
{
    /// <summary>
    /// Name of the solution used for the shared smoke.
    /// </summary>
    public const string SolutionName = "solutionArea";

    /// <summary>
    /// If set, adds the provided reagents to the initial smoke entity spawned.
    /// If spawned via other smoke, these reagents are not set.
    /// </summary>
    [DataField]
    public Solution? StartingContents;

    /// <summary>
    /// The entity containing shared smoke source data and reagents.
    /// If not set, the smoke will not work.
    /// </summary>
    [ViewVariables]
    public Entity<SmokeSourceComponent>? SmokeSourceEntity;

    /// <summary>
    /// The max amount of tiles this smoke cloud can spread to.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int SpreadAmount;

    /// <summary>
    /// The total lifespan of the smoke.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Duration = 10;
}
