using Robust.Shared.Serialization;

namespace Content.Shared._EinsteinEngines.Supermatter.Monitor;

[Serializable, NetSerializable]
public enum SupermatterStatusType : sbyte
{
    Error = -1,
    Inactive = 0,
    Normal = 1,
    Caution = 2,
    Warning = 3,
    Danger = 4,
    Emergency = 5,
    Delaminating = 6
}
