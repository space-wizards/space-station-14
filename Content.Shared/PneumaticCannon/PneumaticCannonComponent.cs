using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.PneumaticCannon;

/// <summary>
///     Handles gas powered guns--cancels shooting if no gas is available, and takes gas from the given container slot.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class PneumaticCannonComponent : Component
{
    public const string TankSlotId = "gas_tank";

    [ViewVariables(VVAccess.ReadWrite)]
    public PneumaticCannonPower Power = PneumaticCannonPower.Medium;

    [DataField("toolModifyPower", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
    public string ToolModifyPower = "Anchoring";

    /// <summary>
    ///     How long to stun for if they shoot the pneumatic cannon at high power.
    /// </summary>
    [DataField("highPowerStunTime")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float HighPowerStunTime = 3.0f;

    /// <summary>
    ///     Amount of moles to consume for each shot at any power.
    /// </summary>
    [DataField("gasUsage")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float GasUsage = 2f;

    /// <summary>
    ///     Base projectile speed at default power.
    /// </summary>
    [DataField("baseProjectileSpeed")]
    public float BaseProjectileSpeed = 20f;
}

/// <summary>
///     How strong the pneumatic cannon should be.
///     Each tier throws items farther and with more speed, but has drawbacks.
///     The highest power knocks the player down for a considerable amount of time.
/// </summary>
public enum PneumaticCannonPower : byte
{
    Low = 0,
    Medium = 1,
    High = 2,
    Len = 3 // used for length calc
}
