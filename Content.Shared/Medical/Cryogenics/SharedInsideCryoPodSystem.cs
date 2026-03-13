using Content.Shared.Standing;
using Content.Shared.Temperature;
using Robust.Shared.Containers;

namespace Content.Shared.Medical.Cryogenics;

public abstract partial class SharedCryoPodSystem
{
    public virtual void InitializeInsideCryoPod()
    {
        SubscribeLocalEvent<InsideCryoPodComponent, EntGotRemovedFromContainerMessage>(OnEntGotRemovedFromContainer);
        SubscribeLocalEvent<InsideCryoPodComponent, DownAttemptEvent>(HandleDown);
        SubscribeLocalEvent<InsideCryoPodComponent, BeforeHeatExchangeEvent>(OnBeforeHeatExchange);
    }

    private void OnEntGotRemovedFromContainer(Entity<InsideCryoPodComponent> entity, ref EntGotRemovedFromContainerMessage args)
    {
        RemCompDeferred<InsideCryoPodComponent>(entity);
    }

    // Must stand in the cryo pod
    private void HandleDown(Entity<InsideCryoPodComponent> entity, ref DownAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnBeforeHeatExchange(Entity<InsideCryoPodComponent> entity, ref BeforeHeatExchangeEvent args)
    {
        args.Conductance *= entity.Comp.ConductanceMod;
    }
}
