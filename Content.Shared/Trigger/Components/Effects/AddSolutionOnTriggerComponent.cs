using Robust.Shared.GameStates;
using Content.Shared.Chemistry.Components;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Adds reagents to the specified solution when the trigger is activated.
/// If TargetUser is true the user will have the solution added instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AddSolutionOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The name of the solution to add to.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public string Solution = string.Empty;

    /// <summary>
    /// The reagent(s) to be added in the solution.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public Solution AddedSolution = default!;
}
