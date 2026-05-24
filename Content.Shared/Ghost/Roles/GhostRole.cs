using Robust.Shared.Serialization;

namespace Content.Shared.Ghost.Roles;

[Serializable, NetSerializable]
public sealed class GhostRole
{
    /// <summary>
    /// Display name sent to the client.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description sent to the client.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Network ID of the ghost role entity.
    /// </summary>
    public NetEntity Id;
}
