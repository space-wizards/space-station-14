using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry;

[Serializable, NetSerializable]
public enum ThermobathVisuals
{
    IsOn,
    IsOff,
    IsHeating,
    IsCooling,
    IsIdle,
    HasBeaker,
    DoesNotHaveBeaker,
}
