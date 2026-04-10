using Content.Shared.Damage.Components;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.Damage.Systems;

public sealed class DamageProtectionBuffSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageProtectionBuffComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<DamageModifierStatusEffectComponent, StatusEffectRelayedEvent<DamageModifyEvent>>(OnDamageModifyStatus);
    }

    private void OnDamageModify(EntityUid uid, DamageProtectionBuffComponent component, DamageModifyEvent args)
    {
        foreach (var modifier in component.Modifiers.Values)
            args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modifier);
    }

    private void OnDamageModifyStatus(Entity<DamageModifierStatusEffectComponent> status, ref StatusEffectRelayedEvent<DamageModifyEvent> args)
    {
        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, status.Comp.Modifier);
    }
}
