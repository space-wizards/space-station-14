using Content.Shared.Cargo.Components;
using Content.Shared.Chat;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.HijackBeacon;

public sealed partial class HijackBeaconSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private AnchorableSystem _anchor = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public readonly SoundSpecifier AnnounceSound = new SoundPathSpecifier("/Audio/Misc/notice1.ogg");
    public readonly SoundSpecifier DeactivateSound = new SoundPathSpecifier("/Audio/Misc/notice2.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HijackBeaconComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
        SubscribeLocalEvent<HijackBeaconComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeLocalEvent<HijackBeaconComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<HijackBeaconComponent, HijackBeaconDeactivateDoAfterEvent>(OnDeactivateDoAfter);
        SubscribeLocalEvent<HijackBeaconComponent, ExaminedEvent>(OnExaminedEvent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveHijackBeaconComponent, HijackBeaconComponent>();
        while (query.MoveNext(out var uid, out var active, out var comp))
        {
            if (_gameTiming.CurTime < active.CompletionTime)
                return;

            switch (comp.Status)
            {
                case HijackBeaconStatus.Armed:
                    HijackFinish((uid, comp));
                    Dirty(uid, comp);
                    break;
                case HijackBeaconStatus.Cooldown:
                    comp.Status = HijackBeaconStatus.AwaitActivate;
                    RemCompDeferred<ActiveHijackBeaconComponent>(uid);
                    Dirty(uid, comp);
                    break;
            }
        }
    }

    #region Event Subs

    /// <summary>
    ///     Deactivate beacon if it gets unanchored(via a bomb or something)
    /// </summary>
    private void OnAnchorChanged(Entity<HijackBeaconComponent> ent, ref AnchorStateChangedEvent args)
    {
        // Unanchoring the beacon deactivates it. This is to prevent people from bombing the tile the beacon is on and running away with it for a free activation.
        if (!args.Anchored && ent.Comp.Status == HijackBeaconStatus.Armed)
            DeactivateBeacon(ent);
    }

    private void OnUnanchorAttempt(Entity<HijackBeaconComponent> entity, ref UnanchorAttemptEvent args)
    {
        if (entity.Comp.Status == HijackBeaconStatus.Armed)
            args.Cancel();
    }

    /// <summary>
    ///     Get the activation and deactivation verbs.
    /// </summary>
    private void OnGetAltVerbs(Entity<HijackBeaconComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands is null)
            return;

        var user = args.User;

        switch (ent.Comp.Status)
        {
            case HijackBeaconStatus.AwaitActivate:
                args.Verbs.Add(new()
                {
                    Act = () =>
                    {
                        ActivateBeacon(ent, user);
                    },
                    Text = Loc.GetString("hijack-beacon-verb-activate-text"),
                    Message = Loc.GetString("hijack-beacon-verb-activate-message"),
                    Disabled = ent.Comp.Status != HijackBeaconStatus.AwaitActivate || !CanActivate(ent.Owner),
                    TextStyleClass = "InteractionVerb",
                    Impact = LogImpact.High,
                });
                break;
            case HijackBeaconStatus.Armed:
                args.Verbs.Add(new()
                {
                    Act = () =>
                    {
                        DeactivateBeaconDoAfter(ent, user);
                    },
                    Text = Loc.GetString("hijack-beacon-verb-deactivate-text"),
                    Message = Loc.GetString("hijack-beacon-verb-deactivate-message"),
                    TextStyleClass = "InteractionVerb",
                    Impact = LogImpact.High,
                });
                break;
        }
    }

    /// <summary>
    ///     When it's examined.
    /// </summary>
    private void OnExaminedEvent(Entity<HijackBeaconComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        switch (ent.Comp.Status)
        {
           case HijackBeaconStatus.AwaitActivate:
               args.PushMarkup(Loc.GetString("hijack-beacon-examine-await-activate"));
               break;
           case HijackBeaconStatus.Armed:
               if (GetRemainingTime(ent.Owner) is not { } time)
                   return;

               args.PushMarkup(Loc.GetString("defusable-examine-live",
                   ("name", ent),
                   ("time", time)));
               break;
           case HijackBeaconStatus.Cooldown:
               args.PushMarkup(Loc.GetString("hijack-beacon-examine-await-cooldown"));
               break;
           case HijackBeaconStatus.HijackComplete:
               args.PushMarkup(Loc.GetString("hijack-beacon-examine-await-hijack-complete"));
               break;
        }
    }

    /// <summary>
    ///     What happens when you deactivate the beacon.
    /// </summary>
    private void OnDeactivateDoAfter(Entity<HijackBeaconComponent> ent, ref HijackBeaconDeactivateDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        DeactivateBeacon(ent, args.User);

        args.Handled = true;
    }

    #endregion

    /// <summary>
    ///     Arming the beacon. Should only occur if on the ATS.
    /// </summary>
    private void ActivateBeacon(Entity<HijackBeaconComponent> ent, EntityUid? user)
    {
        if (ent.Comp.Status != HijackBeaconStatus.AwaitActivate)
            return;

        var xform = Transform(ent);

        if (!CanActivate((ent, xform)))
            return;

        // Activate and start countdown.
        // Remaining time is adjusted by current time to simplify the update loop.
        EnsureComp<ActiveHijackBeaconComponent>(ent, out var activeComp);
        activeComp.CompletionTime = _gameTiming.CurTime + ent.Comp.HackTime;
        ent.Comp.Status = HijackBeaconStatus.Armed;

        //global announcement
        var sender = Loc.GetString("hijack-beacon-announcement-sender");
        var message = Loc.GetString("hijack-beacon-announcement-activated", ("time", GetRemainingTime((ent.Owner, activeComp)) ?? 0));
        _chat.DispatchGlobalAnnouncement(message, sender, true, AnnounceSound, Color.Yellow);

        //Anchor. Anchoring is tied to activation.
        Anchor((ent, xform), user);

        Dirty(ent);
    }

    /// <summary>
    ///     Deactivates the beacon.
    /// </summary>
    private void DeactivateBeacon(Entity<HijackBeaconComponent> ent, EntityUid? user = null)
    {
        if (ent.Comp.Status != HijackBeaconStatus.Armed)
            return;

        var activeComp = EnsureComp<ActiveHijackBeaconComponent>(ent.Owner);

        // Put beacon on cooldown
        activeComp.CompletionTime = ent.Comp.Cooldown + _gameTiming.CurTime;
        ent.Comp.Status = HijackBeaconStatus.Cooldown;

        //global announcement
        var sender = Loc.GetString("hijack-beacon-announcement-sender");
        var message = Loc.GetString("hijack-beacon-announcement-deactivated");
        _chat.DispatchGlobalAnnouncement(message, sender, true, DeactivateSound, Color.Green);

        // Unanchor. we want anchoring to be tied to activation here so we just call this.
        Unanchor(ent.Owner, user);

        Dirty(ent);
        Dirty(ent.Owner, activeComp);
    }

    /// <summary>
    ///     Complete the hijack. The beacon equivalent to a nuke detonation
    /// </summary>
    private void HijackFinish(Entity<HijackBeaconComponent> ent)
    {
        if (ent.Comp.Status != HijackBeaconStatus.Armed)
            return;

        // Hijack is completed and can't be reattempted
        ent.Comp.Status = HijackBeaconStatus.HijackComplete;
        RemCompDeferred<ActiveHijackBeaconComponent>(ent);

        var beaconXForm = Transform(ent);

        // Ensure we are on the trade station still
        if (!TryComp<TradeStationComponent>(beaconXForm.GridUid, out var station))
        {
            Log.Warning($"Trade station hijack tried to succeed on non-trade station grid {beaconXForm.GridUid}!");
            DeactivateBeacon(ent);
            return;
        }

        if (station.Hacked)
        {
            Log.Warning("Hack succeeded on an already hacked trade station!");
            return;
        }

        // Mark the station as hacked.
        station.Hacked = true;
        Dirty(beaconXForm.GridUid.Value, station);

        // Broadcast that the ATS has been hacked.
        var ev = new HijackBeaconSuccessEvent(ent.Comp.Fine);
        RaiseLocalEvent(ref ev);

        //global announcement
        var sender = Loc.GetString("hijack-beacon-announcement-sender");
        var message = Loc.GetString("hijack-beacon-announcement-success", ("fine", ev.Total));
        _chat.DispatchGlobalAnnouncement(message, sender, true, AnnounceSound, Color.Red);

        // Unanchoring must occur after updating the status, or it will disarm the beacon
        Unanchor((ent, beaconXForm));

        Dirty(ent);
    }

    /// <summary>
    ///     Starts the deactivation doafter.
    /// </summary>
    private void DeactivateBeaconDoAfter(Entity<HijackBeaconComponent> beacon, EntityUid user)
    {
        var doAfter = new DoAfterArgs(EntityManager, user, beacon.Comp.DeactivationLength, new HijackBeaconDeactivateDoAfterEvent(), beacon)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        // (try to) start doafter
        _doAfter.TryStartDoAfter(doAfter);
    }

    #region Helpers

    /// <summary>
    ///     Check if the beacon is on the Trade Station and if the Trade Station has not been hijacked already.
    /// </summary>
    private bool CanActivate(Entity<TransformComponent?> ent)
    {
        ent.Comp ??= Transform(ent);
        return TryComp<TradeStationComponent>(ent.Comp.GridUid, out var tradeStation) && !tradeStation.Hacked && _anchor.CanAnchorAt(ent.Owner);
    }

    /// <summary>
    ///     Anchoring helper
    /// </summary>
    private void Anchor(Entity<TransformComponent?> ent, EntityUid? user = null)
    {
        ent.Comp ??= Transform(ent);

        if (ent.Comp.Anchored || ent.Comp.GridUid == null)
            return;

        _transform.AnchorEntity(ent, ent.Comp);
        _popup.PopupPredicted(Loc.GetString("hijack-beacon-popup-anchor"), ent, user);
    }

    /// <summary>
    ///     Unanchoring helper
    /// </summary>
    private void Unanchor(Entity<TransformComponent?> ent, EntityUid? user = null)
    {
        ent.Comp ??= Transform(ent);

        if (!ent.Comp.Anchored)
            return;

        _transform.Unanchor(ent, ent.Comp);
        _popup.PopupPredicted(Loc.GetString("hijack-beacon-popup-unanchor"), ent, user);
    }

    /// <summary>
    ///     Returns remaining time as an integer so it can be parsed for localization.
    /// </summary>
    private int? GetRemainingTime(Entity<ActiveHijackBeaconComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return null; // Mature error code

        return (int) (ent.Comp.CompletionTime - _gameTiming.CurTime).TotalSeconds;
    }

    #endregion
}

[Serializable, NetSerializable]
public enum HijackBeaconStatus : byte
{
    AwaitActivate,
    Armed,
    Cooldown,
    HijackComplete
}

/// <summary>
///     DoAfter event raised when the hijack beacon is deactivated.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class HijackBeaconDeactivateDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
///     Event raised when the hijack beacon succeeds in hijacking the ATS.
/// </summary>
[ByRefEvent]
public record struct HijackBeaconSuccessEvent(int Fine)
{
    public int Total = 0;
};
