using Content.Server.GameTicking.Rules.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using System.Threading;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.StationEvents.Events;

public sealed class PowerGridCheckRule : StationEventSystem<PowerGridCheckRuleComponent>
{
    [Dependency] private readonly ApcSystem _apc = default!;
    [Dependency] private readonly StationSystem _station = default!;

    protected override void Started(EntityUid uid, PowerGridCheckRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        var query = EntityQueryEnumerator<ApcComponent, TransformComponent>();
        while (query.MoveNext(out var entity, out var apc, out var transform))
        {
            if (apc.MainBreakerEnabled && _station.GetOwningStation(entity, transform) == chosenStation)
            {
                component.Powered.Add(entity);
            }
        }

        RobustRandom.Shuffle(component.Powered);

        component.NumberPerSecond = Math.Max(1, (int)(component.Powered.Count / component.SecondsUntilOff)); // Number of APCs to turn off every second. At least one.
        component.Station = chosenStation.Value;
    }

    protected override void Ended(EntityUid uid, PowerGridCheckRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        foreach (var entity in component.Unpowered)
        {
            if (Deleted(entity))
                continue;

            if (TryComp(entity, out ApcComponent? apc))
            {
                if (!apc.MainBreakerEnabled)
                    _apc.ApcToggleBreaker(entity, apc);
            }
        }

        // Can't use the default EndAudio
        component.AnnounceCancelToken?.Cancel();
        component.AnnounceCancelToken = new CancellationTokenSource();
        Timer.Spawn(3000, () =>
        {
            Audio.PlayGlobal("/Audio/Announcements/power_on.ogg", Filter.Broadcast(), true, AudioParams.Default.WithVolume(-4f));
        }, component.AnnounceCancelToken.Token);
        component.Unpowered.Clear();
    }

    protected override void ActiveTick(EntityUid uid, PowerGridCheckRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        var updates = 0;
        component.FrameTimeAccumulator += frameTime;
        if (component.FrameTimeAccumulator > component.UpdateRate)
        {
            updates = (int) (component.FrameTimeAccumulator / component.UpdateRate);
            component.FrameTimeAccumulator -= component.UpdateRate * updates;
        }

        for (var i = 0; i < updates; i++)
        {
            if (component.Powered.Count == 0)
                break;

            var selected = component.Powered.Pop();
            if (Deleted(selected))
                continue;

            if (TryComp<ApcComponent>(selected, out var apc))
            {
                if (apc.MainBreakerEnabled)
                    _apc.ApcToggleBreaker(selected, apc);
            }

            component.Unpowered.Add(selected);
        }
    }
}
