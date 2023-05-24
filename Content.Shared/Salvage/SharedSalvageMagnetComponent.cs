using Content.Shared.Construction.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Salvage;

public abstract class SharedSalvageMagnetComponent : Component
{
    /// <summary>
    /// The machine part that affects the hold time
    /// </summary>
    [DataField("machinePartHoldTime", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string MachinePartHoldTime = "Capacitor";

    /// <summary>
    /// A multiplier applied to the hold time for each level of <see cref="MachinePartHoldTime"/>
    /// </summary>
    [DataField("partRatingHoldTime"), ViewVariables(VVAccess.ReadWrite)]
    public float PartRatingHoldTime = 1.25f;
}

[Serializable, NetSerializable]
public enum SalvageMagnetVisuals : byte
{
    ChargeState,
    Ready,
    ReadyBlinking,
    Unready,
    UnreadyBlinking
}
