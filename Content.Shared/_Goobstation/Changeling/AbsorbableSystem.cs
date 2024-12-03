using Content.Shared.Changeling;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Atmos.Rotting;

namespace Content.Shared.Changeling;

public sealed partial class SharedAbsorbableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AbsorbableComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<AbsorbableComponent> ent, ref ExaminedEvent args)
    {
        var reducedBiomass = false;
        if (!HasComp<RottingComponent>(ent.Owner) && TryComp<AbsorbableComponent>(ent.Owner, out var comp) && comp.ReducedBiomass)
            reducedBiomass = true;

        if (HasComp<ChangelingComponent>(args.Examiner) && !HasComp<AbsorbedComponent>(ent.Owner) && reducedBiomass)
        {
            args.PushMarkup(Loc.GetString("changeling-examine-reduced-biomass", ("target", Identity.Entity(ent.Owner, EntityManager))));
        }
    }
}
