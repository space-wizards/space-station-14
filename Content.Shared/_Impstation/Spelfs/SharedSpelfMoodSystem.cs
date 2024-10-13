using Content.Shared.Emag.Systems;
using Content.Shared._Impstation.Spelfs.Components;

namespace Content.Shared._Impstation.Spelfs;

public abstract class SharedSpelfMoodSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpelfMoodsComponent, OnAttemptEmagEvent>(OnAttemptEmag);
        SubscribeLocalEvent<SpelfMoodsComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnAttemptEmag(EntityUid uid, SpelfMoodsComponent comp, ref OnAttemptEmagEvent args)
    {
        if (!comp.CanBeEmagged)
            args.Handled = true;
    }

    protected virtual void OnEmagged(EntityUid uid, SpelfMoodsComponent comp, ref GotEmaggedEvent args)
    {
        args.Handled = true;
    }
}
