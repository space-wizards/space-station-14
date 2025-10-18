using System.Collections;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(RefillSmartContainersSystem))]
public sealed partial class SmartSolutionContainerComponent : Component
{
    /// <summary>
    /// The list of reagents that were previously in the container.
    /// This list will be referenced when using the Refill
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly),AutoNetworkedField]
    public List<ReagentQuantity> PreviousContents = [];

    /// <summary>
    /// The Solution that will be refilled and referenced for <see cref="PreviousContents"/>.
    /// </summary>
    [DataField]
    public string SolutionName = "default";

    /// <summary>
    /// Cached Solution for Performance Reasons.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Entity<SolutionComponent>? Solution = null;

    /// <summary>
    /// Cached SolutionManager for Performance Reasons.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public SolutionContainerManagerComponent? SolutionManager = null;
}
