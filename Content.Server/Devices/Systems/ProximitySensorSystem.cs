using System;
using System.Collections.Generic;
using Content.Server.Popups;
using Content.Shared.Devices;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Item;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Timing;

namespace Content.Server.Devices.Systems
{
    //TODO: Make the light on the sensor yellow while arming, and red when on.
    public class ProximitySensorSystem : EntitySystem
    {
        [Dependency]
        private readonly UserInterfaceSystem _userInterfaceSystem = default!;

        [Dependency]
        private readonly IGameTiming _gameTiming = default!;

        [Dependency]
        private readonly SharedBroadphaseSystem _sharedBroadphaseSystem = default!;

        private readonly HashSet<SharedProximitySensorComponent> ActiveComponents = new();

        public override void Initialize()
        {
            SubscribeLocalEvent<SharedProximitySensorComponent, GetOtherVerbsEvent>(AddConfigureVerb);
            SubscribeLocalEvent<SharedProximitySensorComponent, StartCollideEvent>(OnCollide);
            SubscribeLocalEvent<SharedProximitySensorComponent, UseInHandEvent>(OnUse);
            SubscribeLocalEvent<SharedProximitySensorComponent, ComponentStartup>(SyncInitialValues);
            SubscribeLocalEvent<SharedProximitySensorComponent, ComponentShutdown>(OnShutdown);

            // Bound UI subscriptions
            SubscribeLocalEvent<SharedProximitySensorComponent, ProximitySensorUpdateSensorMessage>(OnSensorUpdated);
            SubscribeLocalEvent<SharedProximitySensorComponent, ProximitySensorUpdateActiveMessage>(OnActiveUpdated);
        }

        private void OnShutdown(EntityUid uid, SharedProximitySensorComponent component, ComponentShutdown args)
        {
            ActiveComponents.Remove(component);
        }

        private void OnSensorUpdated(EntityUid uid, SharedProximitySensorComponent proxComponent, ProximitySensorUpdateSensorMessage args)
        {
            //clamp frequency between min and max values.
            var range = Math.Clamp(args.Range, SharedProximitySensorComponent.MinRange,
                SharedProximitySensorComponent.MaxRange);

            var armingTime = Math.Clamp(args.ArmingTime, SharedProximitySensorComponent.MinArmingTime,
                SharedProximitySensorComponent.MaxArmingTime);

            proxComponent.Range = range;
            proxComponent.ArmingTime = armingTime;

            if (EntityManager.TryGetComponent<PhysicsComponent>(uid, out var physComp))
            {
                var fix = physComp.GetFixture(SharedProximitySensorComponent.ProximityTriggerFixture);
                if (fix != null)
                {
                    fix.Shape.Radius = proxComponent.Range;
                    physComp.FixtureChanged(fix);
                }
            }

            var owner = EntityManager.GetEntity(uid);
            if (owner.TryGetContainer(out var container))
            {
                var viewer = container.Owner;
                viewer.PopupMessage(viewer, "The sensor beeps in acknowledgement.");
            }

            UpdateUI(uid, proxComponent);
        }

        private void SyncInitialValues(EntityUid uid, SharedProximitySensorComponent component, ComponentStartup args)
        {
            UpdateUI(uid, component);
        }

        private void OnActiveUpdated(EntityUid uid, SharedProximitySensorComponent component, ProximitySensorUpdateActiveMessage args)
        {
            SetActive(uid, args.Active, component);
        }

        private void OnUse(EntityUid uid, SharedProximitySensorComponent component, UseInHandEvent args)
        {
            ToggleActive(uid, component);
        }

        public void ToggleActive(EntityUid uid, SharedProximitySensorComponent? proxComponent)
        {
            if (!Resolve(uid, ref proxComponent))
                return;

            SetActive(uid, !proxComponent.IsActive, proxComponent);
        }

        public void SetActive(EntityUid uid, bool active, SharedProximitySensorComponent? proxComponent)
        {
            if (!Resolve(uid, ref proxComponent))
                return;

            proxComponent.IsActive = active;

            if (active)
            {
                proxComponent.TimeActivated = _gameTiming.CurTime;
                proxComponent.TimeArmed = proxComponent.TimeActivated + TimeSpan.FromSeconds(proxComponent.ArmingTime);
                ActiveComponents.Add(proxComponent);
            }
            else
            {
                proxComponent.IsArmed = false;
                ActiveComponents.Remove(proxComponent);
            }

            var owner = EntityManager.GetEntity(uid);
            if (owner.TryGetContainer(out var container))
            {
                var viewer = container.Owner;
                viewer.PopupMessage(viewer, proxComponent.IsActive ? "You turn the sensor on." : "You turn the sensor off.");
            }

            UpdateUI(uid, proxComponent);
        }

        private void OnCollide(EntityUid uid, SharedProximitySensorComponent component, StartCollideEvent args)
        {
            //we only care about the trigger box.
            if (args.OurFixture.CollisionLayer != (int) CollisionGroup.MobImpassable)
                return;

            if (!component.IsActive || !component.IsArmed)
                return;

            var owner = EntityManager.GetEntity(uid);
            if (!owner.InRangeUnobstructed(args.OtherFixture.Body.Owner, component.Range))
            {
                //return;
            }

            TriggerSensor(uid);
        }

        public void TriggerSensor(EntityUid uid)
        {
            var owner = EntityManager.GetEntity(uid);
            if (owner.TryGetContainer(out var container))
            {
                RaiseLocalEvent(container.Owner.Uid, new IoDeviceOutputEvent());

                var viewer = container.Owner;
                viewer.PopupMessage(viewer, "The proximity sensor vibrates.");
            }
            else
            {
                owner.PopupMessageEveryone("Bzzzzz...", null, 15);
            }
        }

        private void AddConfigureVerb(EntityUid uid, SharedProximitySensorComponent component, GetOtherVerbsEvent args)
        {
            if (!args.CanAccess)
                return;

            Verb verb = new();
            verb.Act = () =>
            {
                if (!EntityManager.TryGetComponent<ActorComponent>(args.User.Uid, out var actorComponent))
                    return;
                _userInterfaceSystem.TryOpen(uid, ProximitySensorUiKey.Key, actorComponent.PlayerSession);
            };
            verb.Text = "Configure";
            args.Verbs.Add(verb);
        }

        private void UpdateUI(EntityUid uid, SharedProximitySensorComponent? proxComponent)
        {
            if (!Resolve(uid, ref proxComponent))
                return;

            _userInterfaceSystem.TrySetUiState(uid, ProximitySensorUiKey.Key,
                new ProximitySensorBoundUserInterfaceState(proxComponent.Range, proxComponent.IsActive,
                    proxComponent.ArmingTime));
        }

        private void Arm(EntityUid uid, SharedProximitySensorComponent? proxComponent)
        {
            if (!Resolve(uid, ref proxComponent))
                return;

            proxComponent.IsArmed = true;

            if (EntityManager.TryGetComponent<PhysicsComponent>(uid, out var physComp))
            {
                physComp.Awake = true;
                physComp.CanCollide = true;
                _sharedBroadphaseSystem.RegenerateContacts(physComp);
            }
        }

        public override void Update(float frameTime)
        {
            var curTime = _gameTiming.CurTime;
            foreach (var proxComponent in ActiveComponents)
            {
                if (!proxComponent.IsArmed && curTime >= proxComponent.TimeArmed)
                {
                    Arm(proxComponent.Owner.Uid, proxComponent);
                }
            }
        }
    }
}
