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

public sealed class HijackBeaconSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly AnchorableSystem _anchor = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

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
            switch (comp.Status)
            {
                case HijackBeaconStatus.Armed:
                    if (_gameTiming.CurTime < active.CompletionTime)
                        return;

                    HijackFinish((uid, comp));
                    Dirty(uid, comp);
                    break;
                case HijackBeaconStatus.Cooldown:
                    if (comp.CooldownTime < _gameTiming.CurTime)
                    {
                        comp.Status = HijackBeaconStatus.AwaitActivate;
                        RemCompDeferred<ActiveHijackBeaconComponent>(uid);
                        Dirty(uid, comp);
                    }
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

        if (ent.Comp.Status == HijackBeaconStatus.AwaitActivate)
        {
            args.Verbs.Add(new()
            {
                Act = () =>
                {
                    ActivateBeacon(ent);
                },
                Text = Loc.GetString("hijack-beacon-verb-activate-text"),
                Message = Loc.GetString("hijack-beacon-verb-activate-message"),
                Disabled = ent.Comp.Status != HijackBeaconStatus.AwaitActivate || !CanActivate(ent),
                TextStyleClass = "InteractionVerb",
                Impact = LogImpact.High,
            });
        }

        if (ent.Comp.Status == HijackBeaconStatus.Armed)
        {
            var user = args.User;

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
               args.PushMarkup(Loc.GetString("defusable-examine-live",
                   ("name", ent),
                   ("time", GetRemainingTime(ent.Owner))));
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

        DeactivateBeacon(ent);

        args.Handled = true;
    }

    #endregion

    /// <summary>
    ///     Arming the beacon. Should only occur if on the ATS.
    /// </summary>
    private void ActivateBeacon(Entity<HijackBeaconComponent> ent)
    {
        if (ent.Comp.Status != HijackBeaconStatus.AwaitActivate || !CanActivate(ent))
            return;

        // Activate and start countdown.
        // Remaining time is adjusted by current time to simplify the update loop.
        EnsureComp<ActiveHijackBeaconComponent>(ent, out var activeComp);
        activeComp.CompletionTime = _gameTiming.CurTime + ent.Comp.RemainingTime;
        ent.Comp.Status = HijackBeaconStatus.Armed;

        //global announcement
        var sender = Loc.GetString("hijack-beacon-announcement-sender");
        var message = Loc.GetString("hijack-beacon-announcement-activated", ("time", GetRemainingTime((ent.Owner, activeComp))));
        _chat.DispatchGlobalAnnouncement(message, sender, true, AnnounceSound, Color.Yellow);

        //Anchor. Anchoring is tied to activation.
        Anchor(ent);

        Dirty(ent);
    }

    /// <summary>
    ///     Deactivates the beacon.
    /// </summary>
    private void DeactivateBeacon(Entity<HijackBeaconComponent> ent)
    {
        if (ent.Comp.Status != HijackBeaconStatus.Armed)
            return;

        // Put beacon on cooldown
        ent.Comp.CooldownTime = ent.Comp.Cooldown + _gameTiming.CurTime;
        ent.Comp.Status = HijackBeaconStatus.Cooldown;

        //global announcement
        var sender = Loc.GetString("hijack-beacon-announcement-sender");
        var message = Loc.GetString("hijack-beacon-announcement-deactivated");
        _chat.DispatchGlobalAnnouncement(message, sender, true, DeactivateSound, Color.Green);

        // Unanchor. we want anchoring to be tied to activation here so we just call this.
        Unanchor(ent);

        Dirty(ent);
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
        Unanchor(ent, beaconXForm);

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
    private bool CanActivate(Entity<HijackBeaconComponent> ent)
    {
        return TryComp(Transform(ent).GridUid, out TradeStationComponent? tradeStation) && !tradeStation.Hacked && _anchor.CanAnchorAt(ent.Owner);
    }

    /// <summary>
    ///     Anchoring helper
    /// </summary>
    private void Anchor(Entity<HijackBeaconComponent> ent, TransformComponent? beaconXForm = null)
    {
        beaconXForm ??= Transform(ent);

        if (beaconXForm.Anchored || beaconXForm.GridUid == null)
            return;

        _transform.AnchorEntity(ent, beaconXForm);
        _popup.PopupPredicted(Loc.GetString("hijack-beacon-popup-anchor"), ent, null);
    }

    /// <summary>
    ///     Unanchoring helper
    /// </summary>
    private void Unanchor(Entity<HijackBeaconComponent> ent, TransformComponent? beaconXForm = null)
    {
        beaconXForm ??= Transform(ent);

        if (!beaconXForm.Anchored)
            return;

        _transform.Unanchor(ent, beaconXForm);
        _popup.PopupPredicted(Loc.GetString("hijack-beacon-popup-unanchor"), ent, null);
    }

    /// <summary>
    ///     Turns time values into usable values for announcements/examine messages
    /// </summary>
    private int GetRemainingTime(Entity<ActiveHijackBeaconComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return 69420; // Mature error code

        return (int) (ent.Comp.CompletionTime - _gameTiming.CurTime).TotalSeconds;
    }

    #endregion
}

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
