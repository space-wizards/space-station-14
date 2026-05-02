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
        SubscribeLocalEvent<ActiveHackingBeaconComponent, ExaminedEvent>(OnExamine);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveHackingBeaconComponent, StickyComponent>();
        while (query.MoveNext(out var uid, out var hack, out var sticky))
        {
            if (hack.HackCompleted || sticky.StuckTo == null || hack.NextUpdate > _timing.CurTime)
                continue;

            var ev = new HackUpdateEvent();
            ev.Beacon = (uid, hack);
            RaiseLocalEvent(sticky.StuckTo.Value, ev);

            hack.NextUpdate = ev.NextUpdate;
            if (ev.CompleteHack)
            {
                var completeEv = new StructureHackCompletedEvent();
                RaiseLocalEvent(sticky.StuckTo.Value, completeEv);
                hack.HackCompleted = true;
            }
        }
    }

    private void OnAttemptStick(Entity<HackingBeaconComponent> ent, ref AttemptEntityStickEvent args)
    {
        if (!TryComp<BeaconHackableComponent>(args.Target, out var comp))
            return;

        if (comp.Hacked && !comp.Repeatable)
        {
            _popup.PopupPredictedCursor(Loc.GetString("hacking-beacon-already-hacked"), ent);
            args.Cancelled = true;
            return;
        }

        var ev = new AttemptHackStructureEvent();
        ev.Repeat = comp.Hacked;
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

        var activeComp = AddComp<ActiveHackingBeaconComponent>(ent);
        activeComp.TimePlanted = _timing.CurTime;
        Dirty(ent, activeComp);
    }

    private void OnUnstuck(Entity<HackingBeaconComponent> ent, ref EntityUnstuckEvent args)
    {
        // for when something should happen when the beacon is removed, like the ATS hijack.
        var ev = new BeaconRemovedEvent();
        RaiseLocalEvent(args.Target, ev);

        RemComp<ActiveHackingBeaconComponent>(ent);
        Dirty(ent);
    }

    private void OnExamine(Entity<ActiveHackingBeaconComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("hacking-beacon-planted-examined", ("time", (int)(_timing.CurTime - ent.Comp.TimePlanted).TotalSeconds)));
    }
}
