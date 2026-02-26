namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(StaminaDamageOnAppliedStatusEffectSystem))]
public sealed partial class StaminaDamageOnAppliedStatusEffectComponent : Component
{
    [DataField(required: true)]
    public float Damage;
}
