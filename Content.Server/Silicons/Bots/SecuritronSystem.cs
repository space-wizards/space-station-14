using System;
using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Server.Cuffs;
using Content.Server.NPC;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Server.Radio.EntitySystems;
using Content.Server.Silicons.Bots.Components;
using Content.Shared.Chat;
using Content.Shared.CombatMode;
using Content.Shared.Cuffs.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Silicons.Bots;
using Content.Shared.Silicons.Bots.Components;
using Content.Shared.Radio;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Silicons.Bots;

public sealed partial class SecuritronSystem : EntitySystem
{
    // Securitrons are expected to stay within baton range (1.5 tiles) when processing suspects.
    private const float StandbyRange = 1.5f;
    private const float FleeRange = 2.75f;

    private readonly ISawmill _sawmill = Logger.GetSawmill("securitron");

    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly CuffableSystem _cuffable = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;

    private readonly ProtoId<RadioChannelPrototype> _securityChannelId = "Security";
    private RadioChannelPrototype? _securityChannel;
    private bool _loggedMissingSecurityChannel;

    public override void Initialize()
    {
        base.Initialize();

        if (!_prototype.TryIndex(_securityChannelId, out _securityChannel))
        {
            _loggedMissingSecurityChannel = true;
            _sawmill.Error($"Failed to find security radio channel prototype '{_securityChannelId}'. Securitrons will be unable to send status updates over radio.");
        }

        SubscribeLocalEvent<SecuritronComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<SecuritronComponent, HTNComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out var component, out var htn, out var appearance))
        {
            var state = EnsureComp<SecuritronStateComponent>(uid);

            var detain = component.OperatingMode == SecuritronOperatingMode.Detain;
            htn.Blackboard.SetValue(NPCBlackboard.SecuritronDetainModeKey, detain);

            HandleTarget(uid, component, state, htn, now);
            UpdateVisual(uid, component, appearance);
        }
    }

    private void HandleTarget(EntityUid uid, SecuritronComponent component, SecuritronStateComponent state, HTNComponent htn, TimeSpan now)
    {
        // Keep chasing the same suspect even if the planner clears out the blackboard entry between ticks.
        if (!htn.Blackboard.TryGetValue<EntityUid>("Target", out var target, EntityManager) || Deleted(target))
        {
            if (state.CurrentTarget != null && !Deleted(state.CurrentTarget.Value))
            {
                target = state.CurrentTarget.Value;
                EnsureTargetOnBlackboard(htn, target);
                _sawmill.Debug($"Restored cached target {ToPrettyString(target)} for {ToPrettyString(uid)} after HTN reset.");
            }
            else
            {
                _sawmill.Debug($"No valid suspect for {ToPrettyString(uid)}; resetting state machine.");
                ResetTarget(uid, state, htn);
                return;
            }
        }

        if (state.CurrentTarget != target)
            AcquireTarget(uid, state, target, htn, now);

        var botPos = _transform.GetWorldPosition(uid);
        var previousTargetPos = state.LastKnownTargetPosition;
        var targetPos = _transform.GetWorldPosition(target);
        var distance = Vector2.Distance(botPos, targetPos);
        var inRange = distance <= StandbyRange;
        var targetMoved = previousTargetPos != null && Vector2.DistanceSquared(previousTargetPos.Value, targetPos) > 0.01f;

        state.LastKnownTargetPosition = targetPos;
        state.ClosestDistance = MathF.Min(state.ClosestDistance, distance);

        TryComp(target, out CuffableComponent? cuffable);
        var targetCuffed = cuffable?.CuffedHandCount >= 2;

        TryComp<MobStateComponent>(target, out var mobState);
        var targetDowned = mobState != null && mobState.CurrentState > MobState.Alive
                           || HasComp<KnockedDownComponent>(target)
                           || HasComp<StunnedComponent>(target);

        var fleeThreshold = Math.Max(FleeRange, state.ClosestDistance + 1f);
        var fleeing = distance > fleeThreshold && (targetMoved || state.TargetFleeing);

        if (targetDowned || targetCuffed)
            fleeing = false;

        if (fleeing != state.TargetFleeing)
        {
            state.TargetFleeing = fleeing;
            htn.Blackboard.SetValue(NPCBlackboard.SecuritronTargetFleeingKey, fleeing);

            if (fleeing)
                OnSuspectFleeing(uid, state);
        }

        // Drive audible callouts and movement decisions from the engagement state machine.
        switch (state.TargetStatus)
        {
            case SecuritronTargetTrackingState.Announced when inRange:
                AnnounceStandby(uid, state, now);
                break;
            case SecuritronTargetTrackingState.Standby when state.TargetFleeing:
                state.TargetStatus = SecuritronTargetTrackingState.Engaging;
                break;
            case SecuritronTargetTrackingState.Engaging when !state.TargetFleeing:
                if (inRange)
                    AnnounceStandby(uid, state, now);
                else
                    state.TargetStatus = SecuritronTargetTrackingState.Announced;
                break;
        }

        if (targetDowned && state.TargetStatus < SecuritronTargetTrackingState.Downed)
        {
            state.TargetStatus = SecuritronTargetTrackingState.Downed;
            OnSuspectDowned(uid, state);
        }
        else if (!targetDowned && state.TargetStatus == SecuritronTargetTrackingState.Downed && !targetCuffed)
        {
            _sawmill.Debug($"Target {ToPrettyString(target)} recovered before cuffing by {ToPrettyString(uid)}; resuming pursuit.");
            state.CuffInProgress = false;
            state.TargetStatus = state.TargetFleeing
                ? SecuritronTargetTrackingState.Engaging
                : (inRange ? SecuritronTargetTrackingState.Standby : SecuritronTargetTrackingState.Announced);
        }

        if (state.TargetStatus >= SecuritronTargetTrackingState.Downed &&
            state.TargetStatus < SecuritronTargetTrackingState.Cuffed &&
            targetCuffed)
        {
            state.TargetStatus = SecuritronTargetTrackingState.Cuffed;
            OnSuspectCuffed(uid, state);
        }

        // Track whether we are close enough to physically restrain the suspect.
        var withinCuffRange = distance <= StandbyRange;

        if (component.OperatingMode == SecuritronOperatingMode.Arrest &&
            state.TargetStatus >= SecuritronTargetTrackingState.Downed &&
            state.TargetStatus < SecuritronTargetTrackingState.Cuffed &&
            !targetCuffed)
        {
            if (!withinCuffRange)
            {
                state.CuffInProgress = false;
                state.NextCuffAttempt = now;
            }
            else
            {
                if (state.CuffInProgress && now >= state.NextCuffAttempt)
                    state.CuffInProgress = false;

                TryStartCuff(uid, state, target);
            }
        }

        // Only mark the target as subdued once they are restrained (or we are actively cuffing them).
        var targetSubdued = state.TargetStatus switch
        {
            SecuritronTargetTrackingState.Downed => targetCuffed || (component.OperatingMode == SecuritronOperatingMode.Arrest
                ? (withinCuffRange && state.CuffInProgress)
                : withinCuffRange),
            SecuritronTargetTrackingState.Cuffed => true,
            SecuritronTargetTrackingState.Standby => !state.TargetFleeing,
            _ => false,
        };
        SetTargetSubdued(htn, targetSubdued);
    }

    private void AcquireTarget(EntityUid uid, SecuritronStateComponent state, EntityUid target, HTNComponent htn, TimeSpan now)
    {
        state.CurrentTarget = target;
        state.TargetStatus = SecuritronTargetTrackingState.Announced;
        state.TargetFleeing = false;
        state.ReportedSpotted = false;
        state.ReportedFleeing = false;
        state.ReportedDowned = false;
        state.ReportedCuffed = false;
        state.NextSpeechTime = now;
        state.ClosestDistance = float.MaxValue;
        state.LastKnownTargetPosition = null;
        state.CuffInProgress = false;
        state.NextCuffAttempt = TimeSpan.Zero;

        _sawmill.Debug($"Acquired target {ToPrettyString(target)} for {ToPrettyString(uid)} at {state.LastKnownTargetPosition}.");

        htn.Blackboard.SetValue(NPCBlackboard.SecuritronTargetFleeingKey, false);
        SetTargetSubdued(htn, false);

        Speak(uid, state, "securitron-say-halt", now);

        if (!state.ReportedSpotted)
        {
            var location = FormatLocation(uid);
            SendSecurityRadio(uid, "securitron-radio-location", location);
            state.ReportedSpotted = true;
        }
    }

    private void ResetTarget(EntityUid uid, SecuritronStateComponent state, HTNComponent htn)
    {
        state.CurrentTarget = null;
        state.TargetStatus = SecuritronTargetTrackingState.None;
        state.TargetFleeing = false;
        state.ReportedSpotted = false;
        state.ReportedFleeing = false;
        state.ReportedDowned = false;
        state.ReportedCuffed = false;
        state.ClosestDistance = float.MaxValue;
        state.LastKnownTargetPosition = null;
        state.CuffInProgress = false;
        state.NextCuffAttempt = TimeSpan.Zero;

        _sawmill.Debug($"Resetting target state for {ToPrettyString(uid)}.");

        htn.Blackboard.SetValue(NPCBlackboard.SecuritronTargetFleeingKey, false);
        SetTargetSubdued(htn, false);
    }

    private void AnnounceStandby(EntityUid uid, SecuritronStateComponent state, TimeSpan now)
    {
        if (state.TargetStatus == SecuritronTargetTrackingState.Standby)
            return;

        state.TargetStatus = SecuritronTargetTrackingState.Standby;
        Speak(uid, state, "securitron-say-standby", now);
    }

    private void OnSuspectFleeing(EntityUid uid, SecuritronStateComponent state)
    {
        if (state.ReportedFleeing)
            return;

        state.ReportedFleeing = true;
        state.TargetStatus = SecuritronTargetTrackingState.Engaging;

        _sawmill.Debug($"Target fleeing from {ToPrettyString(uid)}; switching to Engage state.");

        Speak(uid, state, "securitron-say-fleeing", _timing.CurTime);

        var location = FormatLocation(uid);
        SendSecurityRadio(uid, "securitron-radio-fleeing", location);
    }


    private void OnSuspectDowned(EntityUid uid, SecuritronStateComponent state)
    {
        StopCombat(uid);
        state.CuffInProgress = false;
        state.NextCuffAttempt = _timing.CurTime;

        if (state.ReportedDowned)
            return;

        _sawmill.Debug($"Target downed for {ToPrettyString(uid)}; preparing to cuff.");

        state.ReportedDowned = true;
        var location = FormatLocation(uid);
        SendSecurityRadio(uid, "securitron-radio-downed", location);
    }

    private void OnSuspectCuffed(EntityUid uid, SecuritronStateComponent state)
    {
        StopCombat(uid);
        state.CuffInProgress = false;

        if (state.ReportedCuffed)
            return;

        _sawmill.Debug($"Target cuffed by {ToPrettyString(uid)}; broadcasting status.");

        state.ReportedCuffed = true;
        var location = FormatLocation(uid);
        SendSecurityRadio(uid, "securitron-radio-cuffed", location);
    }

    private void UpdateVisual(EntityUid uid, SecuritronComponent component, AppearanceComponent appearance)
    {
        var state = SecuritronVisualState.Online;

        if (TryComp<CombatModeComponent>(uid, out var combat) && combat.IsInCombatMode || HasComp<NPCMeleeCombatComponent>(uid))
            state = SecuritronVisualState.Combat;

        if (state == component.CurrentState)
            return;

        component.CurrentState = state;
        _appearance.SetData(uid, SecuritronVisuals.State, state, appearance);
    }

    /// <summary>
    /// Starts (or resumes) the cuffing do-after once the securitron is in range and hands are prepared.
    /// </summary>
    private void TryStartCuff(EntityUid uid, SecuritronStateComponent state, EntityUid target)
    {
        var now = _timing.CurTime;

        if (state.CuffInProgress && now < state.NextCuffAttempt)
        {
            _sawmill.Debug($"Cuff do-after still pending for {ToPrettyString(uid)} targeting {ToPrettyString(target)}.");
            return;
        }

        if (state.CuffInProgress && now >= state.NextCuffAttempt)
            state.CuffInProgress = false;

        if (!TryComp(target, out CuffableComponent? cuffable) || cuffable.CuffedHandCount >= 2)
        {
            _sawmill.Debug($"Cannot cuff target {ToPrettyString(target)}; valid cuffable component missing or already restrained.");
            return;
        }

        if (!TryComp(uid, out HandsComponent? hands))
        {
            _sawmill.Warning($"{ToPrettyString(uid)} attempted to cuff without hands component present.");
            return;
        }

        EntityUid? cuffs = null;
        foreach (var held in _hands.EnumerateHeld((uid, hands)))
        {
            if (!HasComp<HandcuffComponent>(held))
                continue;

            cuffs = held;
            break;
        }

        if (cuffs == null)
        {
            try
            {
                cuffs = Spawn("Zipties", Transform(uid).Coordinates);
                _sawmill.Debug($"Spawned spare zipties for {ToPrettyString(uid)} before cuffing {ToPrettyString(target)}.");
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to spawn zipties for {ToPrettyString(uid)} while cuffing {ToPrettyString(target)}: {ex}");
                return;
            }

            if (!_hands.TryPickupAnyHand(uid, cuffs.Value, checkActionBlocker: false, animate: false))
            {
                QueueDel(cuffs.Value);
                _sawmill.Warning($"{ToPrettyString(uid)} failed to pick up spawned zipties while cuffing {ToPrettyString(target)}.");
                return;
            }
        }

        state.CuffInProgress = true;
        state.NextCuffAttempt = now + TimeSpan.FromSeconds(3);

        if (!_cuffable.TryCuffing(uid, target, cuffs.Value))
        {
            state.CuffInProgress = false;
            _sawmill.Debug($"Cuff do-after could not start for {ToPrettyString(uid)} targeting {ToPrettyString(target)}.");
        }
        else
        {
            _sawmill.Debug($"Cuff do-after started for {ToPrettyString(uid)} targeting {ToPrettyString(target)}.");
        }
    }

    /// <summary>
    /// Sends localized speech (or emotes) while throttling repeat lines.
    /// </summary>
    private void Speak(EntityUid uid, SecuritronStateComponent state, string key, TimeSpan now)
    {
        if (now < state.NextSpeechTime)
            return;

        var message = Loc.GetString(key);
        _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, hideChat: false, hideLog: false);

        state.NextSpeechTime = now + TimeSpan.FromSeconds(2);
    }

    /// <summary>
    /// Keeps the HTN blackboard in sync with the server-side perception of whether our suspect is compliant.
    /// </summary>
    private void SetTargetSubdued(HTNComponent htn, bool value)
    {
        if (htn.Blackboard.TryGetValue<bool>(NPCBlackboard.SecuritronTargetSubduedKey, out var current, EntityManager) && current == value)
            return;

        htn.Blackboard.SetValue(NPCBlackboard.SecuritronTargetSubduedKey, value);
    }

    private void StopCombat(EntityUid uid)
    {
        _combatMode.SetInCombatMode(uid, false);

        if (HasComp<NPCMeleeCombatComponent>(uid))
            RemComp<NPCMeleeCombatComponent>(uid);
    }

    /// <summary>
    /// Rebuilds the HTN blackboard state for the current target so subsequent HTN ticks keep pathing correctly.
    /// </summary>
    private void EnsureTargetOnBlackboard(HTNComponent htn, EntityUid target)
    {
        var coords = _transform.GetMoverCoordinates(target);
        htn.Blackboard.SetValue("Target", target);
        htn.Blackboard.SetValue("TargetCoordinates", coords);
    }

    private string FormatLocation(EntityUid uid)
    {
        var coordinates = _transform.GetWorldPosition(uid);
        var x = MathF.Round(coordinates.X);
        var y = MathF.Round(coordinates.Y);
        return Loc.GetString("securitron-location-generic", ("x", x), ("y", y));
    }

    private void SendSecurityRadio(EntityUid uid, string key, string location)
    {
        if (_securityChannel == null)
        {
            if (!_loggedMissingSecurityChannel)
            {
                _loggedMissingSecurityChannel = true;
                _sawmill.Warning($"Skipping security radio broadcast from {ToPrettyString(uid)} because channel '{_securityChannelId}' is unavailable.");
            }

            return;
        }

        var message = Loc.GetString(key, ("location", location));
        _radio.SendRadioMessage(uid, message, _securityChannel, uid);
    }

    private void OnGetAlternativeVerbs(EntityUid uid, SecuritronComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var nextMode = component.OperatingMode == SecuritronOperatingMode.Arrest
            ? SecuritronOperatingMode.Detain
            : SecuritronOperatingMode.Arrest;

        var nextModeName = Loc.GetString(GetModeLocale(nextMode));

        var verb = new AlternativeVerb
        {
            Text = Loc.GetString("securitron-verb-set-mode", ("mode", nextModeName)),
            Icon = new SpriteSpecifier.Texture(new ResPath("Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () =>
            {
                component.OperatingMode = nextMode;
                Dirty(uid, component);

                var modeName = Loc.GetString(GetModeLocale(component.OperatingMode));
                _popup.PopupEntity(Loc.GetString("securitron-popup-mode-changed", ("mode", modeName)), uid, args.User);
            }
        };

        args.Verbs.Add(verb);
    }

    private static string GetModeLocale(SecuritronOperatingMode mode)
    {
        return mode switch
        {
            SecuritronOperatingMode.Arrest => "securitron-mode-name-arrest",
            SecuritronOperatingMode.Detain => "securitron-mode-name-detain",
            _ => "securitron-mode-name-arrest",
        };
    }
}
