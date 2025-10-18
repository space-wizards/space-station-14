using Content.Shared.Body.Events;

namespace Content.Shared.Traits.Assorted;

public sealed class HemophiliaSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<HemophiliaComponent, BleedModifierEvent>(OnBleedModifier);
    }

    private void OnBleedModifier(Entity<HemophiliaComponent> ent, ref BleedModifierEvent args)
    {
        args.BleedReductionAmount *= ent.Comp.HemophiliaBleedReductionMultiplier;
    }
}
