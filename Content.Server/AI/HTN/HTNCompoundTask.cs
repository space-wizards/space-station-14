using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.AI.HTN;

/// <summary>
/// Represents a network of multiple tasks. This gets expanded out to its relevant nodes.
/// </summary>
[Prototype("htnCompoundTask")]
public sealed class HTNCompoundTask : HTNTask, ISerializationHooks
{
    /// <summary>
    /// A descriptor of the field, to be used for debugging.
    /// </summary>
    [DataField("desc")] public string? Desc;

    [DataField("graph")] public List<HTNNode> Graph = new();

    public IReadOnlyDictionary<string, HTNNode> NodeMap => _nodeMap;

    private readonly Dictionary<string, HTNNode> _nodeMap = new();

    public void AfterDeserialization()
    {
        foreach (var node in Graph)
        {
            _nodeMap.Add(node.ID, node);
        }
    }
}
