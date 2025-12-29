using Content.Shared._Offbrand.Wounds;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class BrainHealingStatusEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrainHealingStatusEffectComponent, StatusEffectRelayedEvent<BeforeHealBrainDamage>>(OnBeforeHealBrainDamage);
    }

    private void OnBeforeHealBrainDamage(Entity<BrainHealingStatusEffectComponent> ent, ref StatusEffectRelayedEvent<BeforeHealBrainDamage> args)
    {
        args.Args = args.Args with { Heal = true };
    }
}
