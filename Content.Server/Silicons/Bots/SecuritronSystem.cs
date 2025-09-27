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
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Silicons.Bots;

public sealed partial class SecuritronSystem : EntitySystem
{
    private const float StandbyRange = 1.5f;
    private const float FleeRange = 2.75f;

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
    private RadioChannelPrototype _securityChannel = default!;

    public override void Initialize()
    {
        base.Initialize();

        _securityChannel = _prototype.Index(_securityChannelId);

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
        if (!htn.Blackboard.TryGetValue<EntityUid>("Target", out var target, EntityManager) || Deleted(target))
        {
            if (state.CurrentTarget != null && !Deleted(state.CurrentTarget.Value))
            {
                target = state.CurrentTarget.Value;
                EnsureTargetOnBlackboard(htn, target);
            }
            else
            {
                ResetTarget(state, htn);
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

    private void ResetTarget(SecuritronStateComponent state, HTNComponent htn)
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

    private void TryStartCuff(EntityUid uid, SecuritronStateComponent state, EntityUid target)
    {
        if (state.CuffInProgress || _timing.CurTime < state.NextCuffAttempt)
            return;

        if (!TryComp(target, out CuffableComponent? cuffable) || cuffable.CuffedHandCount >= 2)
            return;

        if (!TryComp(uid, out HandsComponent? hands))
            return;

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
            cuffs = Spawn("Zipties", Transform(uid).Coordinates);

            if (!_hands.TryPickupAnyHand(uid, cuffs.Value, checkActionBlocker: false, animate: false))
            {
                QueueDel(cuffs.Value);
                return;
            }
        }

        state.CuffInProgress = true;
        state.NextCuffAttempt = _timing.CurTime + TimeSpan.FromSeconds(3);

        if (!_cuffable.TryCuffing(uid, target, cuffs.Value))
        {
            state.CuffInProgress = false;
        }
    }

    private void Speak(EntityUid uid, SecuritronStateComponent state, string key, TimeSpan now)
    {
        if (now < state.NextSpeechTime)
            return;

        var message = Loc.GetString(key);
        _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, hideChat: false, hideLog: false);

        state.NextSpeechTime = now + TimeSpan.FromSeconds(2);
    }

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





