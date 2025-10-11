using Content.Shared.Damage;

namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(MeleeModifiersStatusEffectSystem))]
public sealed partial class MeleeModifiersStatusEffectComponent : Component
{
    [DataField]
    public DamageModifierSet? DamageModifier;

    [DataField]
    public float AttackRateMultiplier = 1f;

    [DataField]
    public float AttackRateConstant = 0f;
}
