using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared.Electrocution;

/// <summary>
///     Component for things that shock users on touch.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ElectrifiedComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Should player get damage on collide
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OnBump = true;

    /// <summary>
    /// Should player get damage on attack
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OnAttacked = true;

    /// <summary>
    /// When true - disables power if a window is present in the same tile
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NoWindowInTile = false;

    /// <summary>
    /// Should player get damage on interact with empty hand
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OnHandInteract = true;

    /// <summary>
    /// Should player get damage on interact while holding an object in their hand
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OnInteractUsing = true;

    /// <summary>
    /// Indicates if the entity requires power to function
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequirePower = true;

    /// <summary>
    /// Indicates if the entity uses APC power
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UsesApcPower = false;

    /// <summary>
    /// Identifier for the high voltage node.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? HighVoltageNode;

    /// <summary>
    /// Identifier for the medium voltage node.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? MediumVoltageNode;

    /// <summary>
    /// Identifier for the low voltage node.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? LowVoltageNode;

    /// <summary>
    /// Damage multiplier for HV electrocution
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HighVoltageDamageMultiplier = 3f;

    /// <summary>
    /// Shock time multiplier for HV electrocution
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HighVoltageTimeMultiplier = 2f;

    /// <summary>
    /// Damage multiplier for MV electrocution
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MediumVoltageDamageMultiplier = 2f;

    /// <summary>
    /// Shock time multiplier for MV electrocution
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MediumVoltageTimeMultiplier = 1.5f;

    [DataField, AutoNetworkedField]
    public float ShockDamage = 7.5f;

    /// <summary>
    /// Shock time, in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ShockTime = 5f;

    [DataField, AutoNetworkedField]
    public float SiemensCoefficient = 1f;

    [DataField, AutoNetworkedField]
    public SoundSpecifier ShockNoises = new SoundCollectionSpecifier("sparks");

    [DataField, AutoNetworkedField]
    public SoundPathSpecifier AirlockElectrifyDisabled = new("/Audio/Machines/airlock_electrify_on.ogg");

    [DataField, AutoNetworkedField]
    public SoundPathSpecifier AirlockElectrifyEnabled = new("/Audio/Machines/airlock_electrify_off.ogg");

    [DataField, AutoNetworkedField]
    public bool PlaySoundOnShock = true;

    [DataField, AutoNetworkedField]
    public float ShockVolume = 20;

    [DataField, AutoNetworkedField]
    public float Probability = 1f;

    [DataField, AutoNetworkedField]
    public bool IsWireCut = false;
}
