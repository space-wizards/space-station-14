using Robust.Shared.Audio;

namespace Content.Server.Electrocution;

/// <summary>
///     Component for things that shock users on touch.
/// </summary>
[RegisterComponent]
public sealed class ElectrifiedComponent : Component
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
    public bool RequirePower { get; } = true;

    [DataField("usesApcPower")]
    public bool UsesApcPower { get; } = false;

    [DataField("highVoltageNode")]
    public string? HighVoltageNode { get; }

    [DataField("mediumVoltageNode")]
    public string? MediumVoltageNode { get; }

    [DataField("lowVoltageNode")]
    public string? LowVoltageNode { get; }

    [DataField("highVoltageDamageMultiplier")]
    public float HighVoltageDamageMultiplier { get; } = 3f;

    [DataField("highVoltageTimeMultiplier")]
    public float HighVoltageTimeMultiplier { get; } = 1.5f;

    [DataField("mediumVoltageDamageMultiplier")]
    public float MediumVoltageDamageMultiplier { get; } = 2f;

    [DataField("mediumVoltageTimeMultiplier")]
    public float MediumVoltageTimeMultiplier { get; } = 1.25f;

    [DataField("shockDamage")]
    public int ShockDamage { get; } = 20;

    /// <summary>
    ///     Shock time, in seconds.
    /// </summary>
    [DataField("shockTime")]
    public float ShockTime { get; } = 8f;

    [DataField("siemensCoefficient")]
    public float SiemensCoefficient { get; } = 1f;

    [DataField("shockNoises")]
    public SoundSpecifier ShockNoises { get; } = new SoundCollectionSpecifier("sparks");

    [DataField("playSoundOnShock")]
    public bool PlaySoundOnShock { get; } = true;

    [DataField("shockVolume")]
    public float ShockVolume { get; } = 20;
}
