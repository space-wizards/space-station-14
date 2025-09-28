namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(SlowOnDamageModifierStatusEffectSystem))]
public sealed partial class SlowOnDamageModifierStatusEffectComponent : Component
{
    [DataField(required: true)]
    public float Modifier;
}
