using Content.Shared.Damage.Components;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.Damage.Systems;

public sealed class DamageModifierStatusEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageModifierStatusEffectComponent, StatusEffectRelayedEvent<DamageModifyEvent>>(OnDamageModifyStatus);
    }

    private void OnDamageModifyStatus(Entity<DamageModifierStatusEffectComponent> status, ref StatusEffectRelayedEvent<DamageModifyEvent> args)
    {
        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, status.Comp.Modifiers);
    }
}
