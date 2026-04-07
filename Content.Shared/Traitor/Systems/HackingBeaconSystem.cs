using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Sticky;
using Content.Shared.Sticky.Components;
using Content.Shared.Traitor.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Traitor.Systems;

public sealed partial class HackingBeaconSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HackingBeaconComponent, EntityStuckEvent>(OnStuck);
        SubscribeLocalEvent<HackingBeaconComponent, EntityUnstuckEvent>(OnUnstuck);
        SubscribeLocalEvent<HackingBeaconComponent, AttemptEntityStickEvent>(OnAttemptStick);
        SubscribeLocalEvent<HackingBeaconComponent, ExaminedEvent>(OnExamine);
    }

    private void OnAttemptStick(Entity<HackingBeaconComponent> ent, ref AttemptEntityStickEvent args)
    {
        if (TryComp<BeaconHackableComponent>(args.Target, out var comp) && comp.Hacked)
        {
            _popup.PopupPredictedCursor(Loc.GetString("hacking-beacon-already-hacked"), ent);
            args.Cancelled = true;
        }

        var ev = new AttemptHackStructureEvent();
        RaiseLocalEvent(args.Target, ev);

        if (ev.Cancelled)
            args.Cancelled = true;
    }

    private void OnStuck(Entity<HackingBeaconComponent> ent, ref EntityStuckEvent args)
    {
        // the structure itself will need to listen for this event on its own system and apply the respective effect.
        var ev = new StructureHackedEvent();
        RaiseLocalEvent(args.Target, ev);

        if (TryComp<BeaconHackableComponent>(args.Target, out var hackableComp))
            hackableComp.Hacked = true;
    }

    private void OnUnstuck(Entity<HackingBeaconComponent> ent, ref EntityUnstuckEvent args)
    {
        // for when something should happen when the beacon is removed, like the ATS hijack.
        var ev = new BeaconRemovedEvent();
        RaiseLocalEvent(args.Target, ev);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HackingBeaconComponent>();
        while (query.MoveNext(out var uid, out var hack))
        {
            if (!TryComp<StickyComponent>(uid, out var sticky))
                continue;
            if (sticky.StuckTo == null)
            {
                hack.TimePlanted = TimeSpan.Zero;
                hack.NextUpdate = TimeSpan.Zero;
                hack.HackCompleted = false;
            }
            else
            {
                if (hack.NextUpdate == TimeSpan.Zero) // we're accumulating now so get us up to server time first
                {
                    hack.NextUpdate = _timing.CurTime + hack.UpdateInterval;
                    Dirty(uid, hack);
                }
                if (hack.NextUpdate > _timing.CurTime)
                    continue;
                hack.NextUpdate += hack.UpdateInterval;
                hack.TimePlanted += hack.UpdateInterval;

                Dirty(uid, hack);
            }
        }
    }

    private void OnExamine(Entity<HackingBeaconComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.TimePlanted != TimeSpan.Zero)
            args.PushMarkup(Loc.GetString("hacking-beacon-planted-examined", ("time", (int)ent.Comp.TimePlanted.TotalSeconds)));
    }
}
