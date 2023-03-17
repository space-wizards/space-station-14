using Content.Server.Explosion.EntitySystems;
using Content.Shared.HotPotato;

namespace Content.Server.HotPotato;

public sealed class HotPotatoSystem : SharedHotPotatoSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HotPotatoComponent, ActiveTimerTriggerEvent>(OnActiveTimer);
    }

    private void OnActiveTimer(EntityUid uid, HotPotatoComponent comp, ref ActiveTimerTriggerEvent args)
    {
        EnsureComp<ActiveHotPotatoComponent>(uid);
        comp.CanTransfer = false;
        Dirty(comp);
    }
}
