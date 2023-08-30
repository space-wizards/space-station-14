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

    [DataField("highVoltageDamageMultiplier")]
    public float HighVoltageDamageMultiplier = 3f;

    [DataField("highVoltageTimeMultiplier")]
    public float HighVoltageTimeMultiplier = 1.5f;

    [DataField("mediumVoltageDamageMultiplier")]
    public float MediumVoltageDamageMultiplier = 2f;

    [DataField("mediumVoltageTimeMultiplier")]
    public float MediumVoltageTimeMultiplier = 1.25f;

    [DataField("shockDamage")]
    public int ShockDamage = 20;

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
}
