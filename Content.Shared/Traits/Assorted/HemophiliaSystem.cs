using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.Traits.Assorted;

public sealed class HemophiliaSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<HemophiliaStatusEffectComponent, StatusEffectRelayedEvent<BleedModifierEvent>>(OnBleedModifier);
        SubscribeLocalEvent<HemophiliaStatusEffectComponent, StatusEffectRelayedEvent<ModifyBloodDropletEvent>>(OnBloodDropletModifierEntity);
    }

    private static void OnBleedModifier(Entity<HemophiliaStatusEffectComponent> ent, ref StatusEffectRelayedEvent<BleedModifierEvent> args)
    {
        var ev = args.Args;
        ev.BleedReductionAmount *= ent.Comp.BleedReductionMultiplier;
        ev.BleedAmount *= ent.Comp.BleedAmountMultiplier;
        args.Args = ev;
    }

    private static void OnBloodDropletModifierEntity(Entity<HemophiliaStatusEffectComponent> ent, ref StatusEffectRelayedEvent<ModifyBloodDropletEvent> args)
    {
        var ev = args.Args;
        ev.Modifier *= ent.Comp.BleedAmountMultiplier;
        args.Args = ev;
    }
}
