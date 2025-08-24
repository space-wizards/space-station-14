using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.GameStates;
using Content.Shared.Chemistry.Components;

namespace Content.Shared.Trigger.Components.Effects;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class AddReagentOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The name of the solution to add to.
    /// </summary>
    [DataField("solution", required: true)]
    public string SolutionName = string.Empty;

    /// <summary>
    /// The solution to add reagents to.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? SolutionRef = null;

    /// <summary>
    /// The reagent(s) to be regenerated in the solution.
    /// </summary>
    [DataField(required: true)]
    public Solution Generated = default!;
}
