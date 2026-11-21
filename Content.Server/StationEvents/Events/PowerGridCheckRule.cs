using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events that force a number of APCs off on a station for a period of time.
/// </summary>
/// <seealso cref="PowerGridCheckRuleComponent"/>
[UsedImplicitly]
public sealed partial class PowerGridCheckRule : StationEventSystem<PowerGridCheckRuleComponent>
{
    [Dependency] private ApcSystem _apc = default!;

    protected override void Started(Entity<PowerGridCheckRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        var apcs = Station.GetEntitiesWithComponentOnStation<ApcComponent>(true, out var chosenStation);
        if (chosenStation is null)
            return;

        var powerGridCheck = ent.Comp1;

        SetRelation(ent.Owner, ref powerGridCheck.AffectedStation, chosenStation.Value.Owner);
        var largestGrid = Station.GetLargestGrid(chosenStation.Value.AsNullable());
        if (largestGrid is null)
            return;

        foreach (var apcUid in apcs)
        {
            if (!apcUid.Comp.MainBreakerEnabled)
                continue;

            var apcTransform = Transform(apcUid);
            if (apcTransform.GridUid != largestGrid)
                continue;

            AddRelation(ent.Owner, powerGridCheck.Powered, apcUid.Owner);
        }

        RobustRandom.Shuffle(powerGridCheck.Powered);

        powerGridCheck.NumberPerSecond = Math.Max(1, (int)(powerGridCheck.Powered.Count / powerGridCheck.SecondsUntilOff)); // Number of APCs to turn off every second. At least one.
    }

    /// <summary>
    /// Check if the entity should be affected by an existing
    /// PowerGridCheckRuleComponent and if so, turns off the APC.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnApcStartup(EntityUid apcUid, PowerGridCheckNotifyComponent comp, ComponentStartup args)
    {
        if (!TryComp<ApcComponent>(apcUid, out var apcComp))
            return;

        if (!TryGetRuleAffectingEntity(apcUid, out var rule)
            || !apcComp.MainBreakerEnabled)
            return;

        _apc.ApcToggleBreaker(apcUid, apcComp);
        AddRelation(rule.Value.Owner, rule.Value.Comp.Unpowered, apcUid);
    }

    [SubscribeLocalEvent]
    private void OnApcToggleMainBreaker(EntityUid uid, PowerGridCheckNotifyComponent component, ref ApcToggleMainBreakerAttemptEvent args)
    {
        args.Cancelled |= TryGetRuleAffectingEntity(uid, out _);
    }

    /// <summary>
    /// Returns the PowerGridCheckRuleComponent affecting the uid, or null if none
    /// </summary>
    private bool TryGetRuleAffectingEntity(EntityUid uid, [NotNullWhen(true)] out Entity<PowerGridCheckRuleComponent>? rule)
    {
        rule = null;
        if (!TryComp(uid, out TransformComponent? xform))
            return false;

        if (!TryComp<StationMemberComponent>(xform.GridUid, out var stationMemberComp))
            return false;

        var activeRules = AllEntityQuery<PowerGridCheckRuleComponent, ActiveGameRuleComponent>();
        while (activeRules.MoveNext(out var ruleUid, out var powerGridRule, out _))
        {
            if (powerGridRule.AffectedStation.Entity == null)
                continue;

            if (stationMemberComp.Station != powerGridRule.AffectedStation)
                continue;

            var largestGrid = Station.GetLargestGrid(powerGridRule.AffectedStation.Entity.Value);

            if (largestGrid == null)
                continue;

            if (xform.GridUid != largestGrid.Value)
                continue;

            rule = (ruleUid, powerGridRule);
            return true;
        }

        return false;
    }

    protected override void Ended(Entity<PowerGridCheckRuleComponent> rule, ref GameRuleEndedEvent args)
    {
        base.Ended(rule, ref args);

        foreach (var entity in rule.Comp.Unpowered)
        {
            if (entity.Entity == null)
                continue;

            if (!TryComp<ApcComponent>(entity.Entity.Value, out var apcComponent))
                continue;

            if (!apcComponent.MainBreakerEnabled)
                _apc.ApcToggleBreaker(entity.Entity.Value, apcComponent);
        }

        // Can't use the default EndAudio
        rule.Comp.AnnounceCancelToken?.Cancel();
        rule.Comp.AnnounceCancelToken = new CancellationTokenSource();
        Timer.Spawn(3000,
            () =>
        {
            Audio.PlayGlobal(rule.Comp.PowerOnSound, Filter.Broadcast(), true);
        },
            rule.Comp.AnnounceCancelToken.Token);
        ClearRelations(rule.Owner, rule.Comp.Unpowered);
    }

    protected override void ActiveTick(EntityUid uid, PowerGridCheckRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        var updates = 0;
        component.FrameTimeAccumulator += frameTime;
        if (component.FrameTimeAccumulator > component.UpdateRate)
        {
            updates = (int)(component.FrameTimeAccumulator / component.UpdateRate);
            component.FrameTimeAccumulator -= component.UpdateRate * updates;
        }

        for (var i = 0; i < updates; i++)
        {
            if (component.Powered.Count == 0)
                break;

            var selected = component.Powered.Pop();
            if (selected.Entity == null)
                continue;

            var selectedEnt = selected.Entity.Value;
            ClearRelation(uid, ref selected); // Will be reassigned to another field

            if (TryComp<ApcComponent>(selectedEnt, out var apcComponent))
            {
                if (apcComponent.MainBreakerEnabled)
                    _apc.ApcToggleBreaker(selectedEnt, apcComponent);
            }

            AddRelation(uid, component.Unpowered, selectedEnt);
        }
    }
}
