using Content.Shared.Body.Components;

namespace Content.Shared.Traits.Assorted;

public sealed partial class HemophiliaSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<HemophiliaComponent, EntityBleedEvent>(OnBleedStackReduceEvent);
    }

    private void OnBleedStackReduceEvent(Entity<HemophiliaComponent> ent, ref EntityBleedEvent args)
    {
        args.BleedReductionAmount *= ent.Comp.HemophiliaBleedReductionMultiplier;
    }
}
