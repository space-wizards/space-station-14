using Robust.Shared.Serialization;

namespace Content.Shared.Mind;

[Serializable, NetSerializable]
public enum ToggleableGhostRoleVisuals : byte
{
    Status
}

[Serializable, NetSerializable]
public enum ToggleableGhostRoleStatus : byte
{
    Off,
    Searching,
    On
}
