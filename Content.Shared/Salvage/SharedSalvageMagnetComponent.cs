using Robust.Shared.Serialization;

namespace Content.Shared.Salvage;

public abstract partial class SharedSalvageMagnetComponent : Component {}

[Serializable, NetSerializable]
public enum SalvageMagnetVisuals : byte
{
    ChargeState,
    Ready,
    ReadyBlinking,
    Unready,
    UnreadyBlinking
}
