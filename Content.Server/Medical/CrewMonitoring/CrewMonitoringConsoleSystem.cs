using System.Collections;
using System.Collections.Generic;
using Content.Server.Access.Components;
using Content.Server.Access.Systems;
using Content.Server.Inventory.Components;
using Content.Server.Medical.SuitSensors;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Inventory;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.MobState.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Medical.CrewMonitoring
{
    public class CrewMonitoringConsoleSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
        [Dependency] private readonly IdCardSystem _idCardSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CrewMonitoringConsoleComponent, ActivateInWorldEvent>(OnActivate);
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
        }

        private IEnumerable<CrewMonitoringStatus> GetAllActiveSensors(EntityUid uid, CrewMonitoringConsoleComponent? component = null,
            TransformComponent? transform = null)
        {
            var ret = new List<CrewMonitoringStatus>();
            if (!Resolve(uid, ref component, ref transform))
                return ret;

            var suitSensors = EntityManager.EntityQuery<SuitSensorComponent, TransformComponent>();
            foreach (var (sensor, sensorTransform) in suitSensors)
            {
                if (transform.MapID != sensorTransform.MapID)
                    continue;

                var state = GetSensorState(sensor, transform);
                if (state != null)
                    ret.Add(state);
            }

            return ret;
        }

        private CrewMonitoringStatus? GetSensorState(SuitSensorComponent sensor, TransformComponent transform)
        {
            // check if sensor is enabled and worn by user
            if (sensor.Mode == SuitSensorMode.SensorOff || sensor.User == null)
                return null;

            // try to get mobs id from ID slot
            var userName = Loc.GetString("crew-monitoring-component-unknown-name");
            var userJob = Loc.GetString("crew-monitoring-component-unknown-job");
            if (_idCardSystem.TryGeIdCardSlot(sensor.User.Value, out var card))
            {
                if (card.FullName != null)
                    userName = card.FullName;
                if (card.JobTitle != null)
                    userJob = card.JobTitle;
            }

            // get health mob state
            if (!EntityManager.TryGetComponent(sensor.User.Value, out MobStateComponent? mobState))
                return null;
            var isAlive = mobState.IsAlive();

            // get mob total damage
            if (!EntityManager.TryGetComponent(sensor.User.Value, out DamageableComponent? damageable))
                return null;
            var totalDamage = damageable.TotalDamage;

            // finally, form suit sensor status
            var status = new CrewMonitoringStatus(userName, userJob);
            switch (sensor.Mode)
            {
                case SuitSensorMode.SensorBinary:
                    status.IsAlive = isAlive;
                    break;
                case SuitSensorMode.SensorVitals:
                    status.IsAlive = isAlive;
                    status.TotalDamage = totalDamage;
                    break;
                case SuitSensorMode.SensorCords:
                    status.IsAlive = isAlive;
                    status.TotalDamage = totalDamage;
                    status.Coordinates = transform.MapPosition;
                    break;
            }

            return status;
        }
    }
}
