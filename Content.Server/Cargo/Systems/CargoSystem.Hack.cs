using Content.Server.Cargo.Components;
using Content.Shared.Cargo.Components;
using Content.Shared.Chat;
using Content.Shared.Emag.Systems;
using Content.Shared.Sticky.Components;
using Content.Shared.Traitor;
using Content.Shared.Traitor.Components;
using Robust.Shared.Audio;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    [Dependency] private SharedChatSystem _chat = default!;

    private void InitializeHack()
    {
        SubscribeLocalEvent<CargoPalletComponent, StructureHackedEvent>(OnPalletHack);
        SubscribeLocalEvent<CargoPalletComponent, AttemptHackStructureEvent>(OnAttemptHack);
        SubscribeLocalEvent<CargoPalletComponent, BeaconRemovedEvent>(OnPalletBeaconRemoved);
        SubscribeLocalEvent<CargoPalletComponent, HackUpdateEvent>(UpdateHack);
        SubscribeLocalEvent<CargoPalletComponent, StructureHackCompletedEvent>(OnPalletHackSuccess);
    }


    private void UpdateHack(Entity<CargoPalletComponent> ent, ref HackUpdateEvent args)
    {
        var gridUid = Transform(ent).GridUid;
        if (!TryComp<TradeStationComponent>(gridUid, out var station))
            return;

        args.NextUpdate = _timing.CurTime + station.HackCompletionTime;

        if (_timing.CurTime - args.Beacon.Comp.TimePlanted >= station.HackCompletionTime)
            args.CompleteHack = true;
    }

    /// <summary>
    /// Is the ATS currently being hacked?
    /// </summary>
    /// <returns>Whether the ATS is currently being hacked.</returns>
    public bool IsTradeStationBeingHacked()
    {
        var query = EntityQueryEnumerator<ActiveHackingBeaconComponent, StickyComponent>();
        while (query.MoveNext(out var hack, out var sticky))
        {
            if (HasComp<CargoPalletComponent>(sticky.StuckTo) && !hack.HackCompleted)
                return true;
        }

        return false;
    }

    private void OnPalletHackSuccess(Entity<CargoPalletComponent> ent, ref StructureHackCompletedEvent args)
    {
        // mark ATS as fully hacked
        var gridUid = Transform(ent).GridUid;
        if (!TryComp<TradeStationComponent>(gridUid, out var station))
            return;
        station.HackCompleted = true;
        Dirty(gridUid.Value, station);

        var ev = new HijackBeaconSuccessEvent(station.Fine);
        RaiseLocalEvent(ref ev);

        // mark all pallets as hacked
        var query = EntityQueryEnumerator<BeaconHackableComponent, CargoPalletComponent>();
        while (query.MoveNext(out var uid, out var hack, out _))
        {
            if (hack.Hacked) continue;
            hack.Hacked = true;
            Dirty(uid, hack);
        }

        //global announcement
        var sender = Loc.GetString("hijack-beacon-announcement-sender");
        var message = Loc.GetString("hijack-beacon-announcement-success", ("fine", ev.Total));
        _chat.DispatchGlobalAnnouncement(message, sender, true, station.AnnounceSound, Color.Red);
    }

    private void OnPalletHack(Entity<CargoPalletComponent> ent, ref StructureHackedEvent args)
    {
        var gridUid = Transform(ent).GridUid;
        if (!TryComp<TradeStationComponent>(gridUid, out var station))
            return;
        station.HackCompleted = true;
        Dirty(gridUid.Value, station);

        //global announcement
        var sender = Loc.GetString("hijack-beacon-announcement-sender");
        var message = Loc.GetString("hijack-beacon-announcement-activated", ("time", station.HackCompletionTime.TotalSeconds));
        _chat.DispatchGlobalAnnouncement(message, sender, true, station.AnnounceSound, Color.Yellow);
    }

    private void OnAttemptHack(Entity<CargoPalletComponent> ent, ref AttemptHackStructureEvent args)
    {
        if (!TryComp<TradeStationComponent>(Transform(ent).GridUid, out var station))
            return;
        if (IsTradeStationBeingHacked()) // already being hacked at the moment or has already been.
            args.Cancel();
    }

    private void OnPalletBeaconRemoved(Entity<CargoPalletComponent> ent, ref BeaconRemovedEvent args)
    {
        if (!HasComp<BeaconHackableComponent>(ent))
            return;
        var gridUid = Transform(ent).GridUid;
        if (!TryComp<TradeStationComponent>(gridUid, out var station))
            return;
        if (station.HackCompleted)
            return; // not a disarming

        //global announcement
        var sender = Loc.GetString("hijack-beacon-announcement-sender");
        var message = Loc.GetString("hijack-beacon-announcement-deactivated");
        _chat.DispatchGlobalAnnouncement(message, sender, true, station.DeactivateSound, Color.Green);
    }
}
