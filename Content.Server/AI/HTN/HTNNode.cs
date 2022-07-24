using Robust.Shared.Prototypes;

namespace Content.Server.AI.HTN;

/// <summary>
/// A node in a hierarchical task network.
/// </summary>
[DataDefinition]
public sealed class HTNNode
{
    [ViewVariables, IdDataFieldAttribute]
    public string ID { get; } = default!;

    /// <summary>
    /// The task that this node represents.
    /// </summary>
    [ViewVariables, DataField("task", required: true)]
    public HTNTask Task = default!;

    /// <summary>
    /// Other nodes we connect to. If we have no edges then the graph terminates.
    /// </summary>
    [ViewVariables, DataField("edges")]
    public List<string> Edges = new();

    /// <summary>
    /// The concrete other nodes we can connect to.
    /// </summary>
    public Dictionary<string, HTNNode> EdgeNodes = new();
}
