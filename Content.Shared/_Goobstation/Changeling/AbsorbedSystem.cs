using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;

namespace Content.Shared.Changeling;

public sealed partial class AbsorbedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AbsorbedComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<AbsorbedComponent, MobStateChangedEvent>(OnMobStateChange);
    }

    private void OnExamine(Entity<AbsorbedComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("changeling-absorb-onexamine", ("target", Identity.Entity(ent, EntityManager))));
    }

    private void OnMobStateChange(Entity<AbsorbedComponent> ent, ref MobStateChangedEvent args)
    {
        // in case one somehow manages to dehusk someone
        if (args.NewMobState != MobState.Dead)
            RemComp<AbsorbedComponent>(ent);
    }
}
