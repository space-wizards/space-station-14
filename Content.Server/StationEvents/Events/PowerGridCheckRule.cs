using System.Threading;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
using Content.Server.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.StationEvents.Events
{
    [UsedImplicitly]
    public sealed class PowerGridCheckRule : StationEventSystem<PowerGridCheckRuleComponent>
    {
        [Dependency] private readonly ApcSystem _apcSystem = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;

        protected override void Started(EntityUid uid, PowerGridCheckRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
        {
            base.Started(uid, component, gameRule, args);

            if (!TryGetRandomStation(out var chosenStation))
                return;

            component.AffectedStation = chosenStation.Value;

            var query = AllEntityQuery<ApcComponent, TransformComponent>();
            while (query.MoveNext(out var apcUid, out var apc, out var transform))
            {
                if (apc.MainBreakerEnabled && ApcCanBeAffected((uid, component), (apcUid, apc)))
                    component.Powered.Add(apcUid);
            }

            RobustRandom.Shuffle(component.Powered);

            component.NumberPerSecond = Math.Max(1, (int)(component.Powered.Count / component.SecondsUntilOff)); // Number of APCs to turn off every second. At least one.
        }

        protected override void Ended(EntityUid uid, PowerGridCheckRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
        {
            base.Ended(uid, component, gameRule, args);

            foreach (var entity in component.Unpowered)
            {
                if (Deleted(entity))
                    continue;

                if (TryComp(entity, out ApcComponent? apcComponent))
                {
                    if (!apcComponent.MainBreakerEnabled)
                        _apcSystem.ApcToggleBreaker(entity, apcComponent);
                }
            }

            // Can't use the default EndAudio
            component.AnnounceCancelToken?.Cancel();
            component.AnnounceCancelToken = new CancellationTokenSource();
            Timer.Spawn(3000, () =>
            {
                Audio.PlayGlobal(component.PowerOnSound, Filter.Broadcast(), true);
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
                updates = (int)(component.FrameTimeAccumulator / component.UpdateRate);
                component.FrameTimeAccumulator -= component.UpdateRate * updates;
            }

            for (var i = 0; i < updates; i++)
            {
                if (component.Powered.Count == 0)
                    break;

                var selected = component.Powered.Pop();
                if (Deleted(selected))
                    continue;
                if (TryComp<ApcComponent>(selected, out var apcComponent))
                {
                    if (apcComponent.MainBreakerEnabled)
                        _apcSystem.ApcToggleBreaker(selected, apcComponent);
                }
                component.Unpowered.Add(selected);
            }
        }

        public bool ApcCanBeAffected(Entity<PowerGridCheckRuleComponent> ent, Entity<ApcComponent> apc, TransformComponent? apcXform = null)
        {
            if (!_gameTicker.IsGameRuleActive(ent.Owner))
                return false;

            if (!Resolve(apc.Owner, ref apcXform))
                return false;

            if (!HasComp<BecomesStationComponent>(apcXform.GridUid))
                return false;

            return CompOrNull<StationMemberComponent>(apcXform.GridUid)?.Station == ent.Comp.AffectedStation;
        }

        public bool ContainsUnpoweredApc(Entity<PowerGridCheckRuleComponent> ent, Entity<ApcComponent> apc)
        {
            if (!ApcCanBeAffected(ent, apc))
                return false;

            return ent.Comp.Unpowered.Contains(apc.Owner);
        }

        public bool TryAddUnpoweredApc(Entity<PowerGridCheckRuleComponent> ent, Entity<ApcComponent> apc)
        {
            if (!ApcCanBeAffected(ent, apc))
                return false;

            if (ent.Comp.Powered.Contains(apc.Owner) ||
                ent.Comp.Unpowered.Contains(apc.Owner))
                return false;

            ent.Comp.Unpowered.Add(apc.Owner);

            return true;
        }
    }
}
