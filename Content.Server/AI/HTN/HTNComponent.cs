using Content.Server.AI.Components;

namespace Content.Server.AI.HTN;

[RegisterComponent]
public sealed class HTNComponent : NPCComponent
{
    /// <summary>
    /// The base node to expand for planning.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("rootNode", required: true)]
    public string RootNode = default!;

    /// <summary>
    /// The NPC's current plan.
    /// </summary>
    [ViewVariables]
    public HTNPlan? Plan;
}
