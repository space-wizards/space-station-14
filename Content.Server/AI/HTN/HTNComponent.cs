using Content.Server.AI.Components;

namespace Content.Server.AI.HTN;

[RegisterComponent]
public sealed class HTNComponent : NPCComponent
{
    /// <summary>
    /// The base node to use for planning.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("rootNode", required: true)]
    public HTNNode RootNode = default!;

    /// <summary>
    /// The base task to use for planning
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("rootTask", required: true)]
    public HTNCompoundTask RootTask = default!;

    /// <summary>
    /// The NPC's current plan.
    /// </summary>
    [ViewVariables]
    public HTNPlan? Plan;
}
