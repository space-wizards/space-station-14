using System;
using System.Linq;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.SuitSensors;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Medical.CrewMonitoring;
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
            SubscribeLocalEvent<CrewMonitoringConsoleComponent, PacketSentEvent>(OnPacketReceived);
            SubscribeLocalEvent<CrewMonitoringConsoleComponent, ActivateInWorldEvent>(OnActivate);
        }

        private void OnPacketReceived(EntityUid uid, CrewMonitoringConsoleComponent component, PacketSentEvent args)
        {
            var suitSensor = _sensors.PackageToSuitSensor(args.Data);
            if (suitSensor == null)
                return;

            suitSensor.Timestamp = _gameTiming.CurTime;
            component.ConnectedSensors[suitSensor.SensorId] = suitSensor;
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
            var allSensors = component.ConnectedSensors.Values.ToList();
            var uiState = new CrewMonitoringState(allSensors);
            ui.SetState(uiState);
        }
    }
}
