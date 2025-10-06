namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(MetabolicRateModifierStatusEffectSystem))]
public sealed partial class MetabolicRateModifierStatusEffectComponent : Component
{
    /// <summary>
    /// The modifier applied to the metabolic rate
    /// </summary>
    [DataField(required: true)]
    public float Delta;

    /// <summary>
    /// The minimum metabolic rate that can happen as a result of this status effect
    /// </summary>
    [DataField]
    public float Min = 1f;

    /// <summary>
    /// The maximum metabolic rate that can happen as a result of this status effect
    /// </summary>
    [DataField]
    public float Max = float.PositiveInfinity;
}
