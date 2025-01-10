using Content.Shared.Emag.Systems;
using Content.Shared._Impstation.Thavens.Components;

namespace Content.Shared._Impstation.Thavens;

public abstract class SharedThavenMoodSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThavenMoodsComponent, OnAttemptEmagEvent>(OnAttemptEmag);
        SubscribeLocalEvent<ThavenMoodsComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnAttemptEmag(EntityUid uid, ThavenMoodsComponent comp, ref OnAttemptEmagEvent args)
    {
        if (!comp.CanBeEmagged)
            args.Handled = true;
    }

    protected virtual void OnEmagged(EntityUid uid, ThavenMoodsComponent comp, ref GotEmaggedEvent args)
    {
        args.Handled = true;
    }
}
