using Robust.Shared.Containers;

namespace Content.Shared.HotPotato;

public abstract class SharedHotPotatoSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HotPotatoComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
    }

    private void OnRemoveAttempt(EntityUid uid, HotPotatoComponent comp, ContainerGettingRemovedAttemptEvent args)
    {
        if (!comp.CanTransfer)
            args.Cancel();
    }
}
