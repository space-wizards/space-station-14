using Content.Shared.Construction.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Salvage;

public abstract partial class SharedSalvageMagnetComponent : Component
{
    /// <summary>
    /// The machine part that affects the attaching and cooldown times
    /// </summary>
    [DataField("machinePartDelay", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string MachinePartDelay = "Capacitor";

    /// <summary>
    /// A multiplier applied to the attaching and cooldown times for each level of <see cref="MachinePartDelay"/>
    /// </summary>
    [DataField("partRatingDelay"), ViewVariables(VVAccess.ReadWrite)]
    public float PartRatingDelay = 0.75f;
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
