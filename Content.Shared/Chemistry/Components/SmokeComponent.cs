using Content.Shared.Chemistry.Components.SolutionManager;
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
    public const string SolutionName = "solutionArea";

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
