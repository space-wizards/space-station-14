using Content.Shared.Cuffs.Components;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Robust.Shared.Timing;

namespace Content.Shared.Cuffs;

public sealed class ShowCuffedTimeSystem: EntitySystem
{
    [Dependency] private readonly SharedCuffableSystem _cuffable = default!;
    [Dependency] private readonly GameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        Subs.SubscribeWithRelay<ShowCuffedTimeComponent, CanSeeCuffedTimeEvent>(OnCanSeeCuffedTime);
        SubscribeLocalEvent<CuffableComponent, ExaminedEvent>(OnExamine);
    }

    private void OnCanSeeCuffedTime(EntityUid uid, ShowCuffedTimeComponent comp, ref CanSeeCuffedTimeEvent args)
    {
        args.CanSeeCuffedTime = true;
    }

    private void OnExamine(EntityUid uid, CuffableComponent comp, ExaminedEvent args)
    {
        if (!_cuffable.IsCuffed((uid, comp)))
            return;

        if (comp.CuffedTime is null)
        {
            Log.Warning("An entity is cuffed but the CuffTime wasn't set.");
            return;
        }

        var duration = _timing.CurTime - comp.CuffedTime;
        var minutes = duration.Value.Minutes;
        var seconds = duration.Value.Seconds;
        var identity = Identity.Entity(uid, EntityManager);

        if (minutes == 0)
            args.PushMarkup(Loc.GetString("examine-cuffed-time-seconds", ("identity", identity), ("seconds", seconds)));
        else
            args.PushMarkup(Loc.GetString("examine-cuffed-time-minutes-and-seconds", ("identity", identity), ("minutes", minutes), ("seconds", seconds)));
    }
}

/// <summary>
/// Raised on an entity to see if it can see the time another entity has been cuffed for.
/// </summary>
/// <param name="CanSeeCuffedTime"></param>
[ByRefEvent]
public record struct CanSeeCuffedTimeEvent(bool CanSeeCuffedTime = false) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.EYES;
}
