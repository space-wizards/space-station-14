namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(GunAccuracyStatusEffectSystem))]
public sealed partial class GunAccuracyStatusEffectComponent : Component
{
    [DataField(required: true)]
    public double AngleRangeModifier;
}
