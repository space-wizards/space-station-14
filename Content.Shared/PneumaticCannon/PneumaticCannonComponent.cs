using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Atmos;
using Robust.Shared.Audio;

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

    [DataField("toolModifyPower", customTypeSerializer: typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
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
    /// What gasses are allowed to be used as a propellant. If null all gases are allowed. If there is a single
    /// mol of gas that is not in the list it will refuse to fire.
    /// </summary>
    [DataField]
    public HashSet<Gas>? AllowedGases;

    /// <summary>
    /// Popup that appears when you try to fire and there is not allowed gas <see cref="AllowedGases"/>.
    /// </summary>
    [DataField]
    public LocId MessageNotAllowedGas = "tank-eject-invalid-gas";

    /// <summary>
    /// Popup that appears when there is not enough gas to fire.
    /// </summary>
    [DataField]
    public LocId MessageInsufficientGas = "tank-eject-insufficient-gas";

    /// <summary>
    /// Sound to play when the turnstile denies entry
    /// </summary>
    [DataField]
    public SoundSpecifier? NotAllowedGasSound = new SoundPathSpecifier("/Audio/Machines/airlock_deny.ogg")
    {
        Params = new()
        {
            Volume = -7,
        },
    };
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
