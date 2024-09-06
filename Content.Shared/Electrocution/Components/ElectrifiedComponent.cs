using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared.Electrocution;

/// <summary>
///     Component for things that shock users on touch.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ElectrifiedComponent : Component
{
    [DataField("enabled"), AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Should player get damage on collide
    /// </summary>
    [DataField("onBump"), AutoNetworkedField]
    public bool OnBump = true;

    /// <summary>
    /// Should player get damage on attack
    /// </summary>
    [DataField("onAttacked"), AutoNetworkedField]
    public bool OnAttacked = true;

    /// <summary>
    /// When true - disables power if a window is present in the same tile
    /// </summary>
    [DataField("noWindowInTile"), AutoNetworkedField]
    public bool NoWindowInTile = false;

    /// <summary>
    /// Should player get damage on interact with empty hand
    /// </summary>
    [DataField("onHandInteract"), AutoNetworkedField]
    public bool OnHandInteract = true;

    /// <summary>
    /// Should player get damage on interact while holding an object in their hand
    /// </summary>
    [DataField("onInteractUsing"), AutoNetworkedField]
    public bool OnInteractUsing = true;

    /// <summary>
    /// Indicates if the entity requires power to function
    /// </summary>
    [DataField("requirePower"), AutoNetworkedField]
    public bool RequirePower = true;

    /// <summary>
    /// Indicates if the entity uses APC power
    /// </summary>
    [DataField("usesApcPower"), AutoNetworkedField]
    public bool UsesApcPower = false;

    /// <summary>
    /// Identifier for the high voltage node.
    /// </summary>
    [DataField("highVoltageNode"), AutoNetworkedField]
    public string? HighVoltageNode;

    /// <summary>
    /// Identifier for the medium voltage node.
    /// </summary>
    [DataField("mediumVoltageNode"), AutoNetworkedField]
    public string? MediumVoltageNode;

    /// <summary>
    /// Identifier for the low voltage node.
    /// </summary>
    [DataField("lowVoltageNode"), AutoNetworkedField]
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
    public float HighVoltageTimeMultiplier = 1.5f;

    /// <summary>
    /// Damage multiplier for MV electrocution
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MediumVoltageDamageMultiplier = 2f;

    /// <summary>
    /// Shock time multiplier for MV electrocution
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MediumVoltageTimeMultiplier = 1.25f;

    [DataField("shockDamage"), AutoNetworkedField]
    public float ShockDamage = 7.5f;

    /// <summary>
    /// Shock time, in seconds.
    /// </summary>
    [DataField("shockTime"), AutoNetworkedField]
    public float ShockTime = 8f;

    [DataField("siemensCoefficient"), AutoNetworkedField]
    public float SiemensCoefficient = 1f;

    [DataField("shockNoises"), AutoNetworkedField]
    public SoundSpecifier ShockNoises = new SoundCollectionSpecifier("sparks");

    [DataField("playSoundOnShock"), AutoNetworkedField]
    public bool PlaySoundOnShock = true;

    [DataField("shockVolume"), AutoNetworkedField]
    public float ShockVolume = 20;

    [DataField, AutoNetworkedField]
    public float Probability = 1f;
}
