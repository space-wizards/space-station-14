using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.AI.HTN;

/// <summary>
/// Represents a network of multiple tasks. This gets expanded out to its relevant nodes.
/// </summary>
[Prototype("htnCompoundTask")]
public sealed class HTNCompoundTask : HTNTask
{
    /// <summary>
    /// The available branches for this compound task.
    /// </summary>
    [DataField("branches")] public List<HTNBranch> Branches = new();
}
