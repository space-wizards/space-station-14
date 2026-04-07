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
    [Dependency] private readonly SharedChatSystem _chat = default!;

    public readonly SoundSpecifier AnnounceSound = new SoundPathSpecifier("/Audio/Misc/notice1.ogg");
    public readonly SoundSpecifier DeactivateSound = new SoundPathSpecifier("/Audio/Misc/notice2.ogg");

    private void InitializeHack()
    {
        SubscribeLocalEvent<CargoPalletComponent, StructureHackedEvent>(OnPalletHack);
        SubscribeLocalEvent<CargoPalletComponent, AttemptHackStructureEvent>(OnAttemptHack);
        SubscribeLocalEvent<CargoPalletComponent, BeaconRemovedEvent>(OnPalletBeaconRemoved);
    }

    private void UpdateHack(float frameTime)
    {
        var query = EntityQueryEnumerator<HackingBeaconComponent, StickyComponent>();
        while (query.MoveNext(out var uid, out var hack, out var sticky))
        {
            if (sticky.StuckTo == null || !TryComp<CargoPalletComponent>(sticky.StuckTo, out var pallet))
                continue;

            if (hack.TimePlanted >= pallet.HackCompletionTime && !hack.HackCompleted)
            {
                hack.HackCompleted = true;
                var ev = new StructureHackCompletedEvent();
                RaiseLocalEvent((EntityUid)sticky.StuckTo, ev);
                OnPalletHackSuccess(((EntityUid)sticky.StuckTo, pallet));
            }
        }
    }

    private void OnPalletHackSuccess(Entity<CargoPalletComponent> ent)
    {
        var ev = new HijackBeaconSuccessEvent(ent.Comp.Fine);
        RaiseLocalEvent(ref ev);

        // mark ATS as fully hacked
        if (TryComp<TradeStationComponent>(Transform(ent).GridUid, out var station))
        {
            station.HackCompleted = true;
            if (Transform(ent).GridUid != null)
                Dirty((EntityUid)Transform(ent).GridUid!, station);
        }

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
        _chat.DispatchGlobalAnnouncement(message, sender, true, AnnounceSound, Color.Red);
    }

    private void OnPalletHack(Entity<CargoPalletComponent> ent, ref StructureHackedEvent args)
    {
        if (Transform(ent).GridUid != null && TryComp<TradeStationComponent>(Transform(ent).GridUid, out var station))
        {
            station.Hacked = true;
            Dirty((EntityUid)Transform(ent).GridUid!, station);
        }

        //global announcement
        var sender = Loc.GetString("hijack-beacon-announcement-sender");
        var message = Loc.GetString("hijack-beacon-announcement-activated", ("time", ent.Comp.HackCompletionTime.TotalSeconds));
        _chat.DispatchGlobalAnnouncement(message, sender, true, AnnounceSound, Color.Yellow);
    }

    private void OnAttemptHack(Entity<CargoPalletComponent> ent, ref AttemptHackStructureEvent args)
    {
        if (!TryComp<TradeStationComponent>(Transform(ent).GridUid, out var station)) return;
        if (station.Hacked) // already being hacked at the moment or has already been.
            args.Cancel();
    }

    private void OnPalletBeaconRemoved(Entity<CargoPalletComponent> ent, ref BeaconRemovedEvent args)
    {
        if (!TryComp<BeaconHackableComponent>(ent, out var hack)) return;
        if (!TryComp<TradeStationComponent>(Transform(ent).GridUid, out var station)) return;
        if (station.HackCompleted) return; // not a disarming

        //global announcement
        var sender = Loc.GetString("hijack-beacon-announcement-sender");
        var message = Loc.GetString("hijack-beacon-announcement-deactivated");
        _chat.DispatchGlobalAnnouncement(message, sender, true, DeactivateSound, Color.Green);

        if (Transform(ent).GridUid == null) return;
        station.Hacked = false;
        Dirty((EntityUid)Transform(ent).GridUid!, station);
    }
}
