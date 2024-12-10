using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;

namespace Content.Shared.Changeling.Devour;

public sealed class ChangelingHuskedCorpseSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingHuskedCorpseComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ChangelingHuskedCorpseComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnExamined(EntityUid uid, ChangelingHuskedCorpseComponent comp, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("changeling-husked-corpse", ("target", Identity.Entity(uid, EntityManager))));
    }

    private void OnMobStateChanged(EntityUid uid, ChangelingHuskedCorpseComponent component,MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            return;
        RemCompDeferred(uid, component);
    }
}
