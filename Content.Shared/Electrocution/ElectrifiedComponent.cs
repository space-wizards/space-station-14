using Robust.Shared.Audio;

namespace Content.Server.Electrocution;

/// <summary>
///     Component for things that shock users on touch.
/// </summary>
[RegisterComponent]
public sealed partial class ElectrifiedComponent : Component
{
    [DataField("enabled")]
    public bool Enabled = true;

    /// <summary>
    /// Should player get damage on collide
    /// </summary>
    [DataField("onBump")]
    public bool OnBump = true;

    /// <summary>
    /// Should player get damage on attack
    /// </summary>
    [DataField("onAttacked")]
    public bool OnAttacked = true;

    /// <summary>
    /// When true - disables power if a window is present in the same tile
    /// </summary>
    [DataField("noWindowInTile")]
    public bool NoWindowInTile = false;

    /// <summary>
    /// Should player get damage on interact with empty hand
    /// </summary>
    [DataField("onHandInteract")]
    public bool OnHandInteract = true;

    /// <summary>
    /// Should player get damage on interact while holding an object in their hand
    /// </summary>
    [DataField("onInteractUsing")]
    public bool OnInteractUsing = true;

    /// <summary>
    /// Indicates if the entity requires power to function
    /// </summary>
    [DataField("requirePower")]
    public bool RequirePower = true;

    /// <summary>
    /// Indicates if the entity uses APC power
    /// </summary>
    [DataField("usesApcPower")]
    public bool UsesApcPower = false;

    /// <summary>
    /// Identifier for the high voltage node.
    /// </summary>
    [DataField("highVoltageNode")]
    public string? HighVoltageNode;

    /// <summary>
    /// Identifier for the medium voltage node.
    /// </summary>
    [DataField("mediumVoltageNode")]
    public string? MediumVoltageNode;

    /// <summary>
    /// Identifier for the low voltage node.
    /// </summary>
    [DataField("lowVoltageNode")]
    public string? LowVoltageNode;

    /// <summary>
    /// Damage multiplier for HV electrocution
    /// </summary>
    [DataField]
    public float HighVoltageDamageMultiplier = 3f;

    /// <summary>
    /// Shock time multiplier for HV electrocution
    /// </summary>
    [DataField]
    public float HighVoltageTimeMultiplier = 1.5f;

    /// <summary>
    /// Damage multiplier for MV electrocution
    /// </summary>
    [DataField]
    public float MediumVoltageDamageMultiplier = 2f;

    /// <summary>
    /// Shock time multiplier for MV electrocution
    /// </summary>
    [DataField]
    public float MediumVoltageTimeMultiplier = 1.25f;

    [DataField("shockDamage")]
    public float ShockDamage = 7.5f;

    /// <summary>
    /// Shock time, in seconds.
    /// </summary>
    [DataField("shockTime")]
    public float ShockTime = 8f;

    [DataField("siemensCoefficient")]
    public float SiemensCoefficient = 1f;

    [DataField("shockNoises")]
    public SoundSpecifier ShockNoises = new SoundCollectionSpecifier("sparks");

    [DataField("playSoundOnShock")]
    public bool PlaySoundOnShock = true;

    [DataField("shockVolume")]
    public float ShockVolume = 20;

    [DataField]
    public float Probability = 1f;
}
