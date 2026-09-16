using Content.Shared.Damage.Components;

namespace Content.Shared.Damage.Systems;

// TODO: can probably kill this, ArmorComponent exists
public sealed partial class DamageModifierStatusEffectSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnDamageModifyStatus(Entity<DamageModifierStatusEffectComponent> status, ref DamageModifyEvent args)
    {
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, status.Comp.Modifiers);
    }
}
