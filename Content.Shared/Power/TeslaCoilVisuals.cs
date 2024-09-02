using Robust.Shared.Serialization;

namespace Content.Shared.Power;

[Serializable, NetSerializable]
public enum TeslaCoilVisuals : byte
{
    Enabled,
    Lightning
}
