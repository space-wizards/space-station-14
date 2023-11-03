
using Content.Shared.Sanity.Components;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Content.Shared.Popups;
using Robust.Shared.Network;


namespace Content.Shared.Sanity;

public sealed class SharedSanitySystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly ITileDefinitionManager _tiledef = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private ISawmill _sawmill = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SanityComponent, EntityUnpausedEvent>(OnSanityUnpause);
    }



    private void OnSanityUnpause(EntityUid uid, SanityComponent component, ref EntityUnpausedEvent args)
    {
        component.NextCheckTime += args.PausedTime;
        Dirty(component);
    }

    public void DoSanityCheck(EntityUid uid, SanityComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!Timing.IsFirstTimePredicted)
            return;

        component.NextCheckTime = Timing.CurTime + component.CheckDuration;
        var ev = new SanityEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var SanityQuery = EntityQueryEnumerator<SanityComponent>();
        while (SanityQuery.MoveNext(out var ent, out var sanity))
        {
            if (Timing.CurTime > sanity.NextCheckTime)
            {
                DoSanityCheck(ent, sanity);
            }
        }

    }

}
