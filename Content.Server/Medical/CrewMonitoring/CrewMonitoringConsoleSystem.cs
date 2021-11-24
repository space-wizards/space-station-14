using System;
using System.Collections.Generic;
using Content.Server.Medical.SuitSensors;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensor;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Server.Medical.CrewMonitoring
{
    public class CrewMonitoringConsoleSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
        [Dependency] private readonly SuitSensorSystem _sensors = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private double _cooldownTime = 10;
        private TimeSpan _cooldownEnd;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CrewMonitoringConsoleComponent, ActivateInWorldEvent>(OnActivate);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var curTime = _gameTiming.CurTime;
            if (_cooldownEnd < curTime)
                return;

            var consoles = EntityManager.EntityQuery<CrewMonitoringConsoleComponent>();
            foreach (var console in consoles)
            {
                UpdateUserInterface(console.OwnerUid, console);
            }

            _cooldownEnd = curTime + TimeSpan.FromSeconds(_cooldownTime);
        }

        private void OnActivate(EntityUid uid, CrewMonitoringConsoleComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            // standard interactions check
            if (!args.InRangeUnobstructed())
                return;
            if (!_actionBlocker.CanInteract(args.User.Uid) || !_actionBlocker.CanUse(args.User.Uid))
                return;

            if (!EntityManager.TryGetComponent(args.User.Uid, out ActorComponent? actor))
                return;

            ShowUI(uid, actor.PlayerSession, component);
            args.Handled = true;
        }

        private void ShowUI(EntityUid uid, IPlayerSession session, CrewMonitoringConsoleComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            var ui = component.Owner.GetUIOrNull(CrewMonitoringUIKey.Key);
            ui?.Open(session);

            UpdateUserInterface(uid, component);
        }

        private void UpdateUserInterface(EntityUid uid, CrewMonitoringConsoleComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            var ui = component.Owner.GetUIOrNull(CrewMonitoringUIKey.Key);
            if (ui == null)
                return;

            // update all sensors info
            var allSensors = GetAllActiveSensors(uid, component);
            var uiState = new CrewMonitoringState(allSensors);
            ui.SetState(uiState);
        }

        private List<SuitSensorStatus> GetAllActiveSensors(EntityUid uid, CrewMonitoringConsoleComponent? component = null,
            TransformComponent? transform = null)
        {
            var ret = new List<SuitSensorStatus>();
            if (!Resolve(uid, ref component, ref transform))
                return ret;

            var suitSensors = EntityManager.EntityQuery<SuitSensorComponent, TransformComponent>();
            foreach (var (sensor, sensorTransform) in suitSensors)
            {
                if (transform.MapID != sensorTransform.MapID)
                    continue;

                var state = _sensors.GetSensorState(sensor.OwnerUid, sensor, sensorTransform);
                if (state != null)
                    ret.Add(state);
            }

            return ret;
        }
    }
}
