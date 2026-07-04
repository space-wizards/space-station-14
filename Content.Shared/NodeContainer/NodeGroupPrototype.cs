using Robust.Shared.Prototypes;

namespace Content.Shared.NodeContainer;

/// <summary>
/// A prototype that defines a type of node group.
/// </summary>
[Prototype]
public sealed partial class NodeGroupPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Numeric ID used to refer to this node group in runtime for performance.
    /// </summary>
    [ViewVariables]
    public ushort GroupId;

    /// <summary>
    /// Description that will show on examination of a node that is a part of this group.
    /// TODO: convert nodes into entities and get description from them instead
    /// </summary>
    [DataField]
    public LocId? NodeDescription;

    [DataField]
    public Color Color { get; private set; } = Color.White;

    public void AssignGroupId(ushort groupId)
    {
        GroupId = groupId;
    }
}
