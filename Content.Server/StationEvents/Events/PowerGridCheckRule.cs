using System.Threading;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.StationEvents.Components;
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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PowerGridCheckNotifyComponent, ComponentStartup>(OnApcStartup);
            SubscribeLocalEvent<PowerGridCheckNotifyComponent, ApcToggleMainBreakerAttemptEvent>(OnApcToggleMainBreaker);
        }

        protected override void Started(EntityUid uid, PowerGridCheckRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
        {
            base.Started(uid, component, gameRule, args);

            if (!TryGetRandomStation(out var chosenStation))
                return;

            component.AffectedStation = chosenStation.Value;

            var query = AllEntityQuery<ApcComponent, TransformComponent>();
            while (query.MoveNext(out var apcUid ,out var apc, out var transform))
            {
                if (apc.MainBreakerEnabled && CompOrNull<StationMemberComponent>(transform.GridUid)?.Station == chosenStation)
                    component.Powered.Add(apcUid);
            }

            RobustRandom.Shuffle(component.Powered);

            component.NumberPerSecond = Math.Max(1, (int)(component.Powered.Count / component.SecondsUntilOff)); // Number of APCs to turn off every second. At least one.

        }

        /// <summary>
        /// Check if the entity should be affected by an existing
        /// PowerGridCheckRuleComponent and if so, turns off the APC.
        /// </summary>
        private void OnApcStartup(EntityUid apcUid, PowerGridCheckNotifyComponent comp, ComponentStartup args)
        {
            if (!TryComp<ApcComponent>(apcUid, out var apcComp))
            {
                return;
            }

            PowerGridCheckRuleComponent? rule = GetRuleAffectingEntity(apcUid);
            if (rule != null && apcComp.MainBreakerEnabled)
            {
                _apcSystem.ApcToggleBreaker(apcUid, apcComp);
                rule.Unpowered.Add(apcUid);
            }
        }

        private void OnApcToggleMainBreaker(EntityUid uid, PowerGridCheckNotifyComponent component, ref ApcToggleMainBreakerAttemptEvent args)
        {
            args.Cancelled |= GetRuleAffectingEntity(uid) != null;
        }

        /// <summary>
        /// Returns the PowerGridCheckRuleComponent affecting the uid, or null if none
        /// </summary>
        private PowerGridCheckRuleComponent? GetRuleAffectingEntity(EntityUid uid)
        {
            if (!TryComp(uid, out TransformComponent? xform))
            {
                return null;
            }

            if (!TryComp<StationMemberComponent>(xform.GridUid, out var stationMemberComp))
            {
                return null;
            }

            var activeRules = AllEntityQuery<PowerGridCheckRuleComponent, ActiveGameRuleComponent>();
            while (activeRules.MoveNext(out var _entity, out var powerGridRule, out var _activeGameRule))
            {
                if (stationMemberComp.Station == powerGridRule.AffectedStation)
                {
                    return powerGridRule;
                }
            }

            return null;
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
                    if(!apcComponent.MainBreakerEnabled)
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
                if (TryComp<ApcComponent>(selected, out var apcComponent))
                {
                    if (apcComponent.MainBreakerEnabled)
                        _apcSystem.ApcToggleBreaker(selected, apcComponent);
                }
                component.Unpowered.Add(selected);
            }
        }
    }
}
