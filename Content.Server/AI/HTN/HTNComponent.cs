using Content.Server.AI.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.AI.HTN;

[RegisterComponent]
public sealed class HTNComponent : NPCComponent
{
    /// <summary>
    /// The base task to use for planning
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite),
     DataField("rootTask", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<HTNCompoundTask>))]
    public string RootTask = default!;

    /// <summary>
    /// The NPC's current plan.
    /// </summary>
    [ViewVariables]
    public HTNPlan? Plan;
}
