using Content.Shared.NodeContainer;

namespace Content.Shared.Electrocution;

public sealed partial class ElectrocutionNode : Node
{
    [DataField("cable")]
    public EntityUid? CableEntity;

    [DataField("node")]
    public string? NodeName;
}
