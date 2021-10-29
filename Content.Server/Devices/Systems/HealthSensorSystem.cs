using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Devices;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Server.Devices.Systems
{
    public class HealthSensorSystem : EntitySystem
    {
        [Dependency]
        private readonly UserInterfaceSystem _userInterfaceSystem = default!;

        private readonly Dictionary<EntityUid, HashSet<SharedHealthSensorComponent>> SensorTargets = new();

        public override void Initialize()
        {
            SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<SharedHealthSensorComponent, AfterInteractEvent>(AfterInteract);
            SubscribeLocalEvent<SharedHealthSensorComponent, ComponentShutdown>(OnHealthSensorShutdown);
            SubscribeLocalEvent<MobStateComponent, ComponentShutdown>(OnMobStateShutdown);
            SubscribeLocalEvent<SharedHealthSensorComponent, UseInHandEvent>(OnUseInHands);
            SubscribeLocalEvent<SharedHealthSensorComponent, GetOtherVerbsEvent>(AddConfigureVerb);

            //Bound UI Events
            SubscribeLocalEvent<SharedHealthSensorComponent, HealthSensorResetMessage>(OnSensorResetMessage);
            SubscribeLocalEvent<SharedHealthSensorComponent, HealthSensorUpdateModeMessage>(OnModeUpdatedMessage);
        }

        private void OnUseInHands(EntityUid uid, SharedHealthSensorComponent component, UseInHandEvent args)
        {
            ToggleActive(uid, component);
        }

        private void AddConfigureVerb(EntityUid uid, SharedHealthSensorComponent component, GetOtherVerbsEvent args)
        {
            if (!args.CanAccess)
                return;

            Verb verb = new();
            verb.Act = () =>
            {
                if (!EntityManager.TryGetComponent<ActorComponent>(args.User.Uid, out var actorComponent))
                    return;
                _userInterfaceSystem.TryOpen(uid, HealthSensorUiKey.Key, actorComponent.PlayerSession);
            };
            verb.Text = "Configure";
            args.Verbs.Add(verb);
        }

        private void OnMobStateShutdown(EntityUid uid, MobStateComponent component, ComponentShutdown args)
        {
            RemoveMobStateFromTargets(uid);
        }

        private void OnHealthSensorShutdown(EntityUid uid, SharedHealthSensorComponent component, ComponentShutdown args)
        {
            RemoveSensor(component);
        }

        private void RemoveMobStateFromTargets(EntityUid mobStateUid)
        {
            SensorTargets.Remove(mobStateUid);
        }

        private void RemoveSensor(SharedHealthSensorComponent component)
        {
            foreach (var targList in SensorTargets.Values)
            {
                targList.Remove(component);
            }
        }

        private void AfterInteract(EntityUid uid, SharedHealthSensorComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach || args.Handled)
                return;

            if (args.Target == null)
                return;

            if (args.Target.TryGetComponent<MobStateComponent>(out var stateComponent))
            {
                args.User.PopupMessage("You attach the health tracker.");
                args.Target.PopupMessage("You feel something prick your skin.");
                TrackMobState(uid, args.Target.Uid, component);
                args.Handled = true;
                return;
            }
        }

        public void TrackMobState(EntityUid uid, EntityUid mobStateUid, SharedHealthSensorComponent? comp)
        {
            if (!Resolve(uid, ref comp))
                return;

            if (!SensorTargets.ContainsKey(mobStateUid))
                SensorTargets.Add(mobStateUid, new());

            SensorTargets[mobStateUid].Add(comp);
        }

        private void OnMobStateChanged(MobStateChangedEvent ev)
        {
            //ping all health sensors that are linked
            if (SensorTargets.TryGetValue(ev.Uid, out var componentList))
            {
                foreach (var component in componentList)
                {
                    if (!component.IsActive)
                        continue;

                    switch (component.Mode)
                    {
                        case SharedHealthSensorComponent.SensorMode.Crit:
                        {
                            if (ev.NewState.IsCritical())
                            {
                                TriggerSensor(component.Owner.Uid, component);
                            }
                            break;
                        }

                        case SharedHealthSensorComponent.SensorMode.Death:
                        {
                            if (ev.NewState.IsDead())
                            {
                                TriggerSensor(component.Owner.Uid, component);
                            }
                            break;
                        }
                    }
                }
            }
        }

        private void OnUse(EntityUid uid, SharedHealthSensorComponent component, UseInHandEvent args)
        {
            ToggleActive(uid, component);
        }

        public void ToggleActive(EntityUid uid, SharedHealthSensorComponent? healthComp)
        {
            if (!Resolve(uid, ref healthComp))
                return;

            SetActive(uid, !healthComp.IsActive, healthComp);
        }

        public void SetActive(EntityUid uid, bool active, SharedHealthSensorComponent? healthComp)
        {
            if (!Resolve(uid, ref healthComp))
                return;

            var oldActive = healthComp.IsActive;
            healthComp.IsActive = active;

            var owner = EntityManager.GetEntity(uid);
            if (owner.TryGetContainer(out var container))
            {
                container.Owner.PopupMessage(healthComp.IsActive ? "You turn the sensor on." : "You turn the sensor off.");
            }

            //if the sensor is suddenly turned on, we should check the state of whatever it's watching.
            //i.e. if the sensor turns on and the target is dead it goes off
            if (active && !oldActive)
            {
                CheckShouldActivate(healthComp);
            }

            UpdateUI(uid, healthComp);
        }

        private void CheckShouldActivate(SharedHealthSensorComponent component)
        {
            if (!component.IsActive)
                return;

            //we find every target of our component
            foreach (var targets in SensorTargets)
            {
                if (!targets.Value.Contains(component))
                    continue;

                if (!EntityManager.TryGetComponent<MobStateComponent>(targets.Key, out var mobState))
                    continue;

                switch (component.Mode)
                {
                    case SharedHealthSensorComponent.SensorMode.Crit:
                    {
                        if (mobState.IsCritical())
                        {
                            TriggerSensor(component.Owner.Uid, component);
                        }
                        break;
                    }

                    case SharedHealthSensorComponent.SensorMode.Death:
                    {
                        if (mobState.IsDead())
                        {
                            TriggerSensor(component.Owner.Uid, component);
                        }
                        break;
                    }
                }
            }
        }

        private void UpdateUI(EntityUid uid, SharedHealthSensorComponent? healthComp)
        {
            if (!Resolve(uid, ref healthComp))
                return;

            _userInterfaceSystem.TrySetUiState(uid, HealthSensorUiKey.Key,
                new HealthSensorBoundUserInterfaceState(healthComp.IsActive, (int) healthComp.Mode));
        }

        private void OnModeUpdatedMessage(EntityUid uid, SharedHealthSensorComponent component, HealthSensorUpdateModeMessage args)
        {
            var vaEnum = (SharedHealthSensorComponent.SensorMode)
                Enum.ToObject(typeof(SharedHealthSensorComponent.SensorMode) , args.Mode);

            var owner = EntityManager.GetEntity(args.Entity);
            //If we've somehow been passed an invalid enum, that probably means the client is cheating.
            if (!Enum.IsDefined(typeof(SharedHealthSensorComponent.SensorMode), vaEnum))
            {
                Logger.Warning($"Received invalid enum for health sensor from {owner}. Suspected cheater.");
                return;
            }

            component.Mode = vaEnum;
            UpdateUI(uid, component);
        }

        private void OnSensorResetMessage(EntityUid uid, SharedHealthSensorComponent component, HealthSensorResetMessage args)
        {
            RemoveSensor(component);

            var owner = EntityManager.GetEntity(uid);
            if (owner.TryGetContainer(out var container))
            {
                container.Owner.PopupMessage("You clear the sensors targets.");
            }
        }

        private void TriggerSensor(EntityUid uid, SharedHealthSensorComponent? component)
        {
            if (!Resolve(uid, ref component))
                return;

            var owner = EntityManager.GetEntity(uid);
            if (owner.TryGetContainer(out var container))
            {
                RaiseLocalEvent(container.Owner.Uid, new IoDeviceOutputEvent());

                container.Owner.PopupMessage("The health sensor vibrates.");
            }
            else
            {
                owner.PopupMessageEveryone("Bzzzzz...", null, 15);
            }
        }
    }
}
