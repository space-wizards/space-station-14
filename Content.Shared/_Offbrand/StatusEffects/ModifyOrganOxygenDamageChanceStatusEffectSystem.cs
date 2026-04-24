using Content.Shared._Offbrand.Organs;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class ModifyOrganOxygenDamageChanceStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly PerfusionSystem _perfusion = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModifyOrganOxygenDamageChanceStatusEffectComponent, StatusEffectRelayedEvent<BeforeDealOrganOxygenDamage>>(OnBeforeDealOrganOxygenDamage);
    }

    private void OnBeforeDealOrganOxygenDamage(Entity<ModifyOrganOxygenDamageChanceStatusEffectComponent> ent, ref StatusEffectRelayedEvent<BeforeDealOrganOxygenDamage> args)
    {
        if (Comp<StatusEffectComponent>(ent).AppliedTo is not { } target)
            return;

        var oxygenation = _perfusion.Spo2(target);

        if (ent.Comp.OxygenationModifierThresholds.LowestMatch(oxygenation) is not { } modifier)
            return;

        args.Args = args.Args with { Chance = args.Args.Chance * modifier };
    }
}
