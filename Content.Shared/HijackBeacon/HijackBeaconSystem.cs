using Content.Shared.Cargo.Components;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Station;
using Content.Shared.Verbs;
using Robust.Shared.Serialization;

namespace Content.Shared.HijackBeacon;

public sealed class HijackBeaconSystem : EntitySystem
{
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        // for getting the 'Activate' verb
        SubscribeLocalEvent<HijackBeaconComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);

        // If we unanchor via grid destruction, deactivate beacon.
        SubscribeLocalEvent<HijackBeaconComponent, AnchorStateChangedEvent>(OnAnchorChanged);

        // Doafters
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
                    TickTimer(ent, frameTime);
                    break;
                case HijackBeaconStatus.COOLDOWN:
                    TickCooldown(ent, frameTime);
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
        // Unanchoring the beacon deactivates it. This is to prevent people from minibombing the tile the beacon is on for a free activation.
        // TODO: Add a check in the if statement to make this not succeed when you just disarm it normally
        if (!args.Anchored && ent.Comp.Status == HijackBeaconStatus.ARMED)
            DeactivateBeacon(ent);
    }

    /// <summary>
    ///     For the verb for activating.
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
                // TODO: Use localization strings
                Text = "Activate",
                Message = "The beacon can only be armed on the Automated Trade Station!",
                Disabled = ent.Comp.Status != HijackBeaconStatus.AWAIT_ACTIVATE || !OnATSCheck(ent),
                TextStyleClass = "InteractionVerb",
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
                // TODO: loc strings
                Text = "Deactivate",
                Message = "Hurry up yo.",
                Disabled = false,
                TextStyleClass = "InteractionVerb",
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

        // TODO: loc strings
        switch (ent.Comp.Status)
        {
           case HijackBeaconStatus.AWAIT_ACTIVATE:
               args.PushMarkup("The beacon is [color=green]ready to activate[/color].");
               break;
           case HijackBeaconStatus.ARMED:
               args.PushMarkup(Loc.GetString("defusable-examine-live",
                   ("name", ent),
                   ("time", Math.Floor(ent.Comp.RemainingTime))));
               break;
           case HijackBeaconStatus.COOLDOWN:
               args.PushMarkup("The beacon is [color=red]on cooldown[/color].");
               break;
           case HijackBeaconStatus.HIJACK_COMPLETE:
               args.PushMarkup("The beacon is [color=red]spent[/color].");
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

    #region Timer Logic

    /// <summary>
    ///     Adjusts the remaining time left on beacon activation, and activates the beacon if it is over.
    /// </summary>
    private void TickTimer(Entity<HijackBeaconComponent> ent, float frameTime)
    {
        ent.Comp.RemainingTime -= frameTime;
        Dirty(ent);

        if (ent.Comp.RemainingTime <= 0)
        {
            ent.Comp.RemainingTime = 0;
            Dirty(ent);
            HijackFinish(ent);
        }
    }

    /// <summary>
    ///     Adjusts the remaining time left on beacon cooldown, and readies the beacon if it is over.
    /// </summary>
    private void TickCooldown(Entity<HijackBeaconComponent> ent, float frameTime)
    {
        ent.Comp.CooldownTime -= frameTime;
        Dirty(ent);

        if (ent.Comp.CooldownTime <= 0)
        {
            ent.Comp.CooldownTime = 0;
            // Since the state handles everything else i think it's okay to just put this here instead of making a new ReadyForActivation method.
            ent.Comp.Status = HijackBeaconStatus.AWAIT_ACTIVATE;
            Dirty(ent);
        }
    }

    #endregion

    /// <summary>
    ///     Arming the beacon. Should only occur if on the ATS.
    /// </summary>
    private void ActivateBeacon(Entity<HijackBeaconComponent> ent)
    {
        if (ent.Comp.Status != HijackBeaconStatus.AWAIT_ACTIVATE || !OnATSCheck(ent))
            return;
        // TODO: Log event

        // Anchor beacon to the ground. Should have to be on the ATS due to other checks.
        var beaconXForm = Transform(ent);

        //play station announcement
        var stationUid = _station.GetStationInMap(beaconXForm.MapID);
        // TODO: Use localization strings
        var message = "WE HIJACKING YOUR ATS";
        var sender = "the beacon";
        _chat.DispatchStationAnnouncement(stationUid ?? ent, message, sender, true, null, Color.Yellow);


        // Activate and start the countdown.
        ent.Comp.Status = HijackBeaconStatus.ARMED;
        ent.Comp.RemainingTime = ent.Comp.Timer;
        Dirty(ent);

        Anchor(ent, beaconXForm);
    }

    /// <summary>
    ///     Deactivates the beacon.
    /// </summary>
    private void DeactivateBeacon(Entity<HijackBeaconComponent> ent)
    {
        if (ent.Comp.Status != HijackBeaconStatus.ARMED)
            return;

        var beaconXForm = Transform(ent);

        // -- TESTING CODE
        var stationUid = _station.GetStationInMap(beaconXForm.MapID);
        var message = "fuck bruh someone disarmed it";
        var sender = "the beacon";
        _chat.DispatchStationAnnouncement(stationUid ?? ent, message, sender, true, null, Color.Yellow);
        // END TESTING CODE

        // Put beacon on cooldown
        ent.Comp.Status = HijackBeaconStatus.COOLDOWN;
        ent.Comp.CooldownTime = ent.Comp.Cooldown;
        Dirty(ent);

        // Unanchor. we want anchoring to be tied to activation here so we just do this.
        Unanchor(ent, beaconXForm);
    }

    /// <summary>
    ///     Complete the hijack. The beacon equivalent to a nuke detonation
    /// </summary>
    private void HijackFinish(Entity<HijackBeaconComponent> ent)
    {
        if (ent.Comp.Status != HijackBeaconStatus.ARMED)
            return;

        // add stuff like announcements, alert level changes, etc here
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

        // -- TESTING CODE
        var stationUid = _station.GetStationInMap(beaconXForm.MapID);
        var message = "ats been hacked, all your spesos are mine";
        var sender = "the beacon";
        _chat.DispatchStationAnnouncement(stationUid ?? ent, message, sender, true, null, Color.Yellow);
        // END TESTING CODE

        DispenseReward(ent);

        ent.Comp.Status = HijackBeaconStatus.HIJACK_COMPLETE;

        Dirty(ent);

        // Unanchoring must occur after updating the status, or it will disarm the beacon
        Unanchor(ent, beaconXForm);
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
    private void Anchor(Entity<HijackBeaconComponent> ent, TransformComponent? beaconXForm)
    {
        beaconXForm ??= Transform(ent);

        if (beaconXForm.Anchored || beaconXForm.GridUid == null)
            return;

        _transform.AnchorEntity(ent, beaconXForm);
        _popup.PopupPredicted("The beacon anchors into the ground!", ent, null);
    }

    /// <summary>
    ///     Unanchoring helper
    /// </summary>
    private void Unanchor(Entity<HijackBeaconComponent> ent, TransformComponent? beaconXForm)
    {
        beaconXForm ??= Transform(ent);

        if (!beaconXForm.Anchored)
            return;

        _transform.Unanchor(ent, beaconXForm);
        _popup.PopupPredicted("The beacon unanchors from the ground.", ent, null);
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
///     Applies to all trade stations, in case of grid splitting.
/// </summary>
public sealed partial class HijackBeaconSuccessEvent : EntityEventArgs
{
}
