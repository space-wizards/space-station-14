using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class DamagedSiliconAccentComponent : Component
{
    /// <summary>
    ///     Enable damage corruption effects
    /// </summary>
    [DataField]
    public bool EnableDamageCorruption = true;

    /// <summary>
    ///     Override total damage for damage corruption effects
    /// </summary>
    [DataField]
    public FixedPoint2? OverrideTotalDamage;

    /// <summary>
    ///     The probability that a character will be corrupted when total damage at or above <see cref="MaxDamageCorruption" />.
    /// </summary>
    [DataField]
    public float MaxDamageCorruption = 0.5f;

    /// <summary>
    ///     Probability of character corruption will increase linearly to <see cref="MaxDamageCorruption" /> once until
    ///     total damage is at or above this value. If null, it will use the value returned by
    ///     DestructibleSystem.DestroyedAt, which is the damage threshold for destruction or breakage.
    /// </summary>
    [DataField]
    public FixedPoint2? DamageAtMaxCorruption;

    /// <summary>
    ///     Enable charge level corruption effects
    /// </summary>
    [DataField]
    public bool EnableChargeCorruption = true;

    /// <summary>
    ///     Override charge level for charge level corruption effects
    /// </summary>
    [DataField]
    public float? OverrideChargeLevel;

    /// <summary>
    ///     If the power cell charge is below this value (as a fraction of maximum charge),
    ///     power corruption will begin to be applied.
    /// </summary>
    [DataField]
    public float ChargeThresholdForPowerCorruption = 0.15f;

    /// <summary>
    ///     Regardless of charge level, this is how many characters at the start of a message will be 100% safe
    ///     from being dropped.
    /// </summary>
    [DataField]
    public int StartPowerCorruptionAtCharIdx = 8;

    /// <summary>
    ///     The probability that a character will be dropped due to charge level will be maximum for characters past
    ///     this index. This has the effect of longer messages dropping more characters later in the message.
    /// </summary>
    [DataField]
    public int MaxPowerCorruptionAtCharIdx = 40;

    /// <summary>
    ///     The maximum probability that a character will be dropped due to charge level.
    /// </summary>
    [DataField]
    public float MaxDropProbFromPower = 0.5f;

    /// <summary>
    ///     If a character is "dropped", this is the probability that the character will be turned into a period instead
    ///     of completely deleting the character.
    /// </summary>
    [DataField]
    public float ProbToCorruptDotFromPower = 0.6f;
}
