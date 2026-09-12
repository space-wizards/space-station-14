using Content.Shared.Body.Events;

namespace Content.Shared.Traits.Assorted;

public sealed partial class HemophiliaSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnBleedModifier(Entity<HemophiliaStatusEffectComponent> ent, ref BleedModifierEvent args)
    {
        args.BleedReductionAmount *= ent.Comp.BleedReductionMultiplier;
        args.BleedAmount *= ent.Comp.BleedAmountMultiplier;
    }
}
