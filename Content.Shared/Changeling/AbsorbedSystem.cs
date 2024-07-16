using Content.Shared.Examine;

namespace Content.Shared.Changeling;

public sealed partial class AbsorbedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AbsorbedComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, AbsorbedComponent comp, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("changeling-absorb-onexamine"));
    }
}
