using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events
{
    [UsedImplicitly]
    public sealed partial class PowerGridCheckRule : StationEventSystem<PowerGridCheckRuleComponent>
    {
        private static readonly EntityTimerId PowerOffTimer = new("power-off");
        private static readonly EntityTimerId PowerOnSoundTimer = new("power-on-sound");

        [Dependency] private ApcSystem _apcSystem = default!;
        [Dependency] private EntityTimerSystem _timers = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PowerGridCheckNotifyComponent, ComponentStartup>(OnApcStartup);
            SubscribeLocalEvent<PowerGridCheckNotifyComponent, ApcToggleMainBreakerAttemptEvent>(OnApcToggleMainBreaker);
            SubscribeLocalEvent<PowerGridCheckRuleComponent, EntityTimerEvent>(OnTimer);
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
            var interval = TimeSpan.FromSeconds(component.UpdateRate);
            _timers.SetTimer<PowerGridCheckRuleComponent>((uid, component), PowerOffTimer, interval, interval);
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

            _timers.CancelTimer<PowerGridCheckRuleComponent>(uid, PowerOffTimer);
            _timers.SetTimer<PowerGridCheckRuleComponent>((uid, component), PowerOnSoundTimer, TimeSpan.FromSeconds(3));
            component.Unpowered.Clear();
        }

        private void OnTimer(Entity<PowerGridCheckRuleComponent> ent, ref EntityTimerEvent args)
        {
            if (args.Id == PowerOnSoundTimer)
            {
                Audio.PlayGlobal(ent.Comp.PowerOnSound, Filter.Broadcast(), true);
                return;
            }

            if (args.Id != PowerOffTimer)
                return;

            for (var i = 0u; i < args.ElapsedCount; i++)
            {
                if (ent.Comp.Powered.Count == 0)
                {
                    _timers.CancelTimer<PowerGridCheckRuleComponent>(ent, PowerOffTimer);
                    break;
                }

                var selected = ent.Comp.Powered.Pop();
                if (Deleted(selected))
                    continue;
                if (TryComp<ApcComponent>(selected, out var apcComponent))
                {
                    if (apcComponent.MainBreakerEnabled)
                        _apcSystem.ApcToggleBreaker(selected, apcComponent);
                }
                ent.Comp.Unpowered.Add(selected);
            }
        }
    }
}
