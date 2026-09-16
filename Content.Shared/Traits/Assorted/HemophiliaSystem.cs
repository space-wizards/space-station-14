using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.Traits.Assorted;

public sealed partial class HemophiliaSystem : EntitySystem
{
    [Dependency] private BloodstreamSystem _bloodstream = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<HemophiliaStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _bloodstream.UpdateBloodDropletTransferAmount(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnRemove(Entity<HemophiliaStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _bloodstream.UpdateBloodDropletTransferAmount(args.Target);
    }

    [SubscribeLocalEvent]
    private static void OnBleedModifier(Entity<HemophiliaStatusEffectComponent> ent, ref StatusEffectRelayedEvent<BleedModifierEvent> args)
    {
        var ev = args.Args;
        ev.BleedReductionAmount *= ent.Comp.BleedReductionMultiplier;
        ev.BleedAmount *= ent.Comp.BleedAmountMultiplier;
        args.Args = ev;
    }

    [SubscribeLocalEvent]
    private static void OnBloodDropletModifierEntity(Entity<HemophiliaStatusEffectComponent> ent, ref StatusEffectRelayedEvent<ModifyBloodDropletEvent> args)
    {
        var ev = args.Args;
        ev.BloodAmount *= ent.Comp.BleedAmountMultiplier;
        args.Args = ev;
    }
}
