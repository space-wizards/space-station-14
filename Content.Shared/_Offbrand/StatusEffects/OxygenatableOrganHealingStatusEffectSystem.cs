using Content.Shared._Offbrand.Organs;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class OxygenatableOrganHealingStatusEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OxygenatableOrganHealingStatusEffectComponent, StatusEffectRelayedEvent<BeforeHealOrganOxygenDamage>>(OnBeforeHealOrganOxygenDamage);
    }

    private void OnBeforeHealOrganOxygenDamage(Entity<OxygenatableOrganHealingStatusEffectComponent> ent, ref StatusEffectRelayedEvent<BeforeHealOrganOxygenDamage> args)
    {
        args.Args = args.Args with { Heal = true };
    }
}
