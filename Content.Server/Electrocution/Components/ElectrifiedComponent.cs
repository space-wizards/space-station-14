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

    [DataField("onBump")]
    public bool OnBump = true;

    [DataField("onAttacked")]
    public bool OnAttacked = true;

    [DataField("noWindowInTile")]
    public bool NoWindowInTile = false;

    [DataField("onHandInteract")]
    public bool OnHandInteract = true;

    [DataField("onInteractUsing")]
    public bool OnInteractUsing = true;

    [DataField("requirePower")]
    public bool RequirePower = true;

    [DataField("usesApcPower")]
    public bool UsesApcPower = false;

    [DataField("highVoltageNode")]
    public string? HighVoltageNode;

    [DataField("mediumVoltageNode")]
    public string? MediumVoltageNode;

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
    ///     Shock time, in seconds.
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
