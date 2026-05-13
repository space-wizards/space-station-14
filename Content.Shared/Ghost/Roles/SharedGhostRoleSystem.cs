using Robust.Shared.Serialization;

namespace Content.Shared.Ghost.Roles;

[Serializable, NetSerializable]
public sealed partial class GhostRole
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public NetEntity Id;
}

