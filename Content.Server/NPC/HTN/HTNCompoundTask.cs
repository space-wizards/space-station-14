using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN;

/// <summary>
/// Represents a network of multiple tasks. This gets expanded out to its relevant nodes.
/// </summary>
[Prototype("htnCompound")]
public sealed class HTNCompoundTask : HTNTask
{
    /// <summary>
    /// The available branches for this compound task.
    /// </summary>
    [DataField("branches", required: true)]
    public List<HTNBranch> Branches = default!;
}
