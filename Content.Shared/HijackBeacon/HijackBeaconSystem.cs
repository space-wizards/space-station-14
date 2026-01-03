using Content.Shared.Cargo.Components;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.HijackBeacon;

public sealed class HijackBeaconSystem : EntitySystem
{
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HijackBeaconComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
        SubscribeLocalEvent<HijackBeaconComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<HijackBeaconComponent, HijackBeaconDeactivateDoAfterEvent>(OnDeactivateDoAfter);
        SubscribeLocalEvent<HijackBeaconComponent, ExaminedEvent>(OnExaminedEvent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HijackBeaconComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            Entity<HijackBeaconComponent> ent = (uid, comp);

            switch (ent.Comp.Status)
            {
                case HijackBeaconStatus.ARMED:
                    if (ent.Comp.RemainingTime < _gameTiming.CurTime)
                    {
                        HijackFinish(ent);
                        Dirty(ent);
                    }
                    break;
                case HijackBeaconStatus.COOLDOWN:
                    if (ent.Comp.CooldownTime < _gameTiming.CurTime)
                    {
                        ent.Comp.Status = HijackBeaconStatus.AWAIT_ACTIVATE;
                        Dirty(ent);
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
        if (!args.Anchored && ent.Comp.Status == HijackBeaconStatus.ARMED)
            DeactivateBeacon(ent);
    }

    /// <summary>
    ///     Get the activation and deactivation verbs.
    /// </summary>
    private void OnGetAltVerbs(Entity<HijackBeaconComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands is null)
            return;

        if (ent.Comp.Status == HijackBeaconStatus.AWAIT_ACTIVATE)
        {
            args.Verbs.Add(new()
            {
                Act = () =>
                {
                    ActivateBeacon(ent);
                },
                Text = Loc.GetString("hijack-beacon-verb-activate-text"),
                Message = Loc.GetString("hijack-beacon-verb-activate-message"),
                Disabled = ent.Comp.Status != HijackBeaconStatus.AWAIT_ACTIVATE || !OnATSCheck(ent),
                TextStyleClass = "InteractionVerb",
                Impact = LogImpact.High,
            });
        }

        if (ent.Comp.Status == HijackBeaconStatus.ARMED)
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
           case HijackBeaconStatus.AWAIT_ACTIVATE:
               args.PushMarkup(Loc.GetString("hijack-beacon-examine-await-activate"));
               break;
           case HijackBeaconStatus.ARMED:
               args.PushMarkup(Loc.GetString("defusable-examine-live",
                   ("name", ent),
                   ("time", AdjustToSeconds(ent.Comp.RemainingTime))));
               break;
           case HijackBeaconStatus.COOLDOWN:
               args.PushMarkup(Loc.GetString("hijack-beacon-examine-await-cooldown"));
               break;
           case HijackBeaconStatus.HIJACK_COMPLETE:
               args.PushMarkup(Loc.GetString("hijack-beacon-examine-await-hijack-complete"));
               break;
        }

        // (un)anchored examine message
        args.PushMarkup(Loc.GetString(Transform(ent).Anchored ? "examinable-anchored" : "examinable-unanchored"));

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
        if (ent.Comp.Status != HijackBeaconStatus.AWAIT_ACTIVATE || !OnATSCheck(ent))
            return;

        // Activate and start countdown.
        // Remaining time is adjusted by current time to simplify the update loop.
        ent.Comp.RemainingTime = _gameTiming.CurTime + ent.Comp.RemainingTime;
        ent.Comp.Status = HijackBeaconStatus.ARMED;

        //global announcement
        var sender = Loc.GetString("hijack-beacon-announcement-sender");
        var message = Loc.GetString("hijack-beacon-announcement-activated", ("time", AdjustToSeconds(ent.Comp.RemainingTime)));
        _chat.DispatchGlobalAnnouncement(message, sender, true, null, Color.Yellow);

        //Anchor. Anchoring is tied to activation.
        Anchor(ent);

        Dirty(ent);
    }

    /// <summary>
    ///     Deactivates the beacon.
    /// </summary>
    private void DeactivateBeacon(Entity<HijackBeaconComponent> ent)
    {
        if (ent.Comp.Status != HijackBeaconStatus.ARMED)
            return;

        // Put beacon on cooldown
        ent.Comp.CooldownTime = ent.Comp.Cooldown + _gameTiming.CurTime;
        ent.Comp.Status = HijackBeaconStatus.COOLDOWN;

        //global announcement
        var sender = Loc.GetString("hijack-beacon-announcement-sender");
        var message = Loc.GetString("hijack-beacon-announcement-deactivated");
        _chat.DispatchGlobalAnnouncement(message, sender, true, null, Color.Green);

        // In case the beacon is re-activated, it has a minimum amount of time before it can go off.
        // Subtracting the current time from the remaining time here so the numbers are the actual time values and not the curTime-adjusted ones.
        ent.Comp.RemainingTime = (ent.Comp.RemainingTime - _gameTiming.CurTime > ent.Comp.MinimumTime ? ent.Comp.RemainingTime - _gameTiming.CurTime : ent.Comp.MinimumTime);

        // Unanchor. we want anchoring to be tied to activation here so we just call this.
        Unanchor(ent);

        Dirty(ent);
    }

    /// <summary>
    ///     Complete the hijack. The beacon equivalent to a nuke detonation
    /// </summary>
    private void HijackFinish(Entity<HijackBeaconComponent> ent)
    {
        if (ent.Comp.Status != HijackBeaconStatus.ARMED)
            return;

        // Hijack is completed and can't be reattempted
        ent.Comp.Status = HijackBeaconStatus.HIJACK_COMPLETE;

        var beaconXForm = Transform(ent);

        // Ensure we are on the trade station still
        if (!HasComp<TradeStationComponent>(beaconXForm.GridUid))
        {
            Log.Warning($"Trade station hijack tried to succeed on non-trade station grid {beaconXForm.GridUid}!");
            DeactivateBeacon(ent);
            return;
        }

        // Raise event on the trade station, so it knows to update hijacked status
        var ev = new HijackBeaconSuccessEvent();
        RaiseLocalEvent((EntityUid)beaconXForm.GridUid, ev);

        //global announcement
        var sender = Loc.GetString("hijack-beacon-announcement-sender");
        var message = Loc.GetString("hijack-beacon-announcement-success");
        _chat.DispatchGlobalAnnouncement(message, sender, true, null, Color.Red);

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
        if (!_doAfter.TryStartDoAfter(doAfter))
            return;
    }

    #region Helpers

    /// <summary>
    ///     When the hijack is completed, dispense a reward for the hijacker.
    /// </summary>
    private void DispenseReward(Entity<HijackBeaconComponent> ent)
    {
        //TODO: Implement a more complex reward that also penalizes the crew in some way to incentivize stopping the hijack

        //for now, we just spawn an entity.
        PredictedSpawnAtPosition(ent.Comp.Reward, Transform(ent).Coordinates);
    }

    /// <summary>
    ///     Check if the beacon is on the Trade Station and if the Trade Station has not been hijacked already.
    /// </summary>
    private bool OnATSCheck(Entity<HijackBeaconComponent> ent)
    {
        return (TryComp(Transform(ent).GridUid, out TradeStationComponent? tradeStation) && !tradeStation.Hacked);
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
    private int AdjustToSeconds(TimeSpan time)
    {
        return (int) (time - _gameTiming.CurTime).TotalSeconds;
    }

    #endregion
}

public enum HijackBeaconStatus : byte
{
    AWAIT_ACTIVATE,
    ARMED,
    COOLDOWN,
    HIJACK_COMPLETE
}

/// <summary>
///     DoAfter event raised when the hijack beacon is deactivated.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class HijackBeaconDeactivateDoAfterEvent : SimpleDoAfterEvent
{
}

/// <summary>
///     Event raised when the hijack beacon succeeds in hijacking the ATS.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class HijackBeaconSuccessEvent : EntityEventArgs
{
}
