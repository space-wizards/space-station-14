using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.PneumaticCannon;

/// <summary>
///     Handles gas powered guns--cancels shooting if no gas is available, and takes gas from the given container slot.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PneumaticCannonComponent : Component
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
    public float GasUsage = 0.142f;

    /// <summary>
    ///     Base projectile speed at default power.
    /// </summary>
    [DataField("baseProjectileSpeed")]
    public float BaseProjectileSpeed = 20f;

    /// <summary>
    ///     The current projectile speed setting.
    /// </summary>
    [DataField]
    public float? ProjectileSpeed;

    /// <summary>
    /// If true, will throw ammo rather than shoot it.
    /// </summary>
    [DataField("throwItems"), ViewVariables(VVAccess.ReadWrite)]
    public bool ThrowItems = true;

    /// <summary>
    ///    How much to multiply the distance by. This is used to make the gun shoot farther on higher Power settings.
    /// </summary>
    [DataField("distanceFactor"), ViewVariables(VVAccess.ReadWrite)]
    public float DistanceFactor = 1.5f;
}

/// <summary>
///    How strong the pneumatic cannon should be.
///    Each tier throws items farther and with more speed, but has drawbacks.
///    Low is like a regular hand throw but with more range,
///    Medium is faster than low and will overshoot the click target because of it,
///    High will knock the player down but is really fast and has increased range.
/// </summary>
public enum PneumaticCannonPower : byte
{
    Low = 0,
    Medium = 1,
    High = 2,
    Len = 3 // used for length calc
}
