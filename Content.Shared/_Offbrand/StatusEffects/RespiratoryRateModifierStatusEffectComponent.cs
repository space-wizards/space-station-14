namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(RespiratoryRateModifierStatusEffectSystem))]
public sealed partial class RespiratoryRateModifierStatusEffectComponent : Component
{
    /// <summary>
    /// The minimum respiratory rate this status effect guarantees
    /// </summary>
    [DataField(required: true)]
    public float Rate;
}
