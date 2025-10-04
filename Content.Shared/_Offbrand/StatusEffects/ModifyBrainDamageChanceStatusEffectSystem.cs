using Content.Shared._Offbrand.Wounds;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class ModifyBrainDamageChanceStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly HeartSystem _heart = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModifyBrainDamageChanceStatusEffectComponent, StatusEffectRelayedEvent<BeforeDealBrainDamage>>(OnBeforeDealBrainDamage);
    }

    private void OnBeforeDealBrainDamage(Entity<ModifyBrainDamageChanceStatusEffectComponent> ent, ref StatusEffectRelayedEvent<BeforeDealBrainDamage> args)
    {
        if (Comp<StatusEffectComponent>(ent).AppliedTo is not { } target)
            return;

        var oxygenation = _heart.BloodOxygenation((target, Comp<HeartrateComponent>(target)));

        if (ent.Comp.OxygenationModifierThresholds.LowestMatch(oxygenation) is not { } modifier)
            return;

        args.Args = args.Args with { Chance = args.Args.Chance * modifier };
    }
}
