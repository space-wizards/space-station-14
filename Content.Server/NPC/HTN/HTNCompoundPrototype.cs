using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN;

/// <summary>
/// Represents a network of multiple tasks. This gets expanded out to its relevant nodes.
/// </summary>
[Prototype("htnCompound")]
public sealed class HTNCompoundPrototype : IPrototype
{
    [DataField("id", required: true)] public string ID { get; } = string.Empty;

    [DataField("branches", required: true, customTypeSerializer:typeof(HTNTaskListSerializer))]
    public List<HTNBranch> Branches = new();
}
