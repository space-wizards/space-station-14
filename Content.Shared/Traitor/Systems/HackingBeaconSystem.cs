using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Sticky;
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
