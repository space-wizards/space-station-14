using Content.Shared.Vehicle.Components;
using Content.Shared.Vehicle;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Server.Light.Components;
using Content.Server.Buckle.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Content.Shared.MobState;
using Content.Shared.Stunnable;
using Content.Server.Hands.Components;
using Content.Server.Hands.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands;
using Robust.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Vehicle
{
    public sealed class VehicleSystem : EntitySystem
    {
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly HandVirtualItemSystem _virtualItemSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VehicleComponent, HonkActionEvent>(OnHonk);
            SubscribeLocalEvent<VehicleComponent, BuckleChangeEvent>(OnBuckleChange);
            SubscribeLocalEvent<VehicleComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<VehicleComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<VehicleComponent, GetVerbsEvent<AlternativeVerb>>(AddKeysVerb);
            SubscribeLocalEvent<VehicleComponent, MoveEvent>(OnMove);
            SubscribeLocalEvent<VehicleComponent, StorageChangedEvent>(OnStorageChanged);
            SubscribeLocalEvent<RiderComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
            SubscribeLocalEvent<RiderComponent, GotParalyzedEvent>(OnParalyzed);
            SubscribeLocalEvent<RiderComponent, MobStateChangedEvent>(OnMobStateChanged);
        }
        /// <summary>
        /// This just controls whether the wheels are turning.
        /// </summary>
        public override void Update(float frameTime)
        {
            foreach (var (vehicle, mover) in EntityQuery<VehicleComponent, SharedPlayerInputMoverComponent>())
            {
                if (mover.VelocityDir.sprinting == Vector2.Zero)
                {
                    UpdateAutoAnimate(vehicle.Owner, false);
                    continue;
                }
                UpdateAutoAnimate(vehicle.Owner, true);
            }
        }

        private void OnComponentInit(EntityUid uid, VehicleComponent component, ComponentInit args)
        {
            var strap = Comp<StrapComponent>(uid);

            component.BaseBuckleOffset = strap.BuckleOffset;
            strap.BuckleOffsetUnclamped = Vector2.Zero; //You're going to align these facing east, so...
            UpdateAppearance(uid, 2);
            UpdateStorageUsed(uid, false);
            _ambientSound.SetAmbience(uid, false);
        }
        /// <summary>
        /// Give the user the rider component if they're buckling to the vehicle,
        /// otherwise remove it.
        /// </summary>
        private void OnBuckleChange(EntityUid uid, VehicleComponent component, BuckleChangeEvent args)
        {
            if (args.Buckling)
            {
                EnsureComp<SharedPlayerInputMoverComponent>(uid);
                var rider = EnsureComp<RiderComponent>(args.BuckledEntity);
                rider.Vehicle = component;
                component.HasRider = true;
                _virtualItemSystem.TrySpawnVirtualItemInHand(uid, args.BuckledEntity);
                UpdateBuckleOffset(Transform(uid), component);
                UpdateAppearance(uid, GetDrawDepth(Transform(uid), component.NorthOnly));
                if (TryComp<ActionsComponent>(args.BuckledEntity, out var actions) && TryComp<UnpoweredFlashlightComponent>(uid, out var flashlight))
                {
                    _actionsSystem.AddAction(args.BuckledEntity, flashlight.ToggleAction, uid, actions);
                }
                if (component.HornSound != null)
                {
                    _actionsSystem.AddAction(args.BuckledEntity, component.HornAction, uid, actions);
                }
                return;
            }
            _actionsSystem.RemoveProvidedActions(args.BuckledEntity, uid);
            _virtualItemSystem.DeleteInHandsMatching(args.BuckledEntity, uid);
            RemComp<RiderComponent>(args.BuckledEntity);
            component.HasRider = false;
        }

        /// <summary>
        /// Handle adding keys to the ignition
        /// </summary>
        private void OnAfterInteractUsing(EntityUid uid, VehicleComponent component, AfterInteractUsingEvent args)
        {
            if (!args.CanReach)
                return;

            if (!HasComp<HandsComponent>(args.User))
                return;

            if (!TryComp<VehicleKeyComponent>(args.Used, out var key))
                return;

            var keyProto = MetaData(key.Owner);

            if (keyProto.EntityPrototype?.ID != component.Key)
                return;

            component.HasKey = true;
            EnsureComp<SharedPlayerInputMoverComponent>(uid);
            SoundSystem.Play(Filter.Pvs(uid), component.StartupSound.GetSound(), uid, AudioParams.Default.WithVolume(1f));
            _ambientSound.SetAmbience(uid, true);
            EntityManager.DeleteEntity(args.Used);
        }
        private void AddKeysVerb(EntityUid uid, VehicleComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess || !HasComp<HandsComponent>(args.User) || !component.HasKey || component.Key == string.Empty)
                return;
            // If someone is riding, let only that person take out the keys.
            if (component.HasRider && !TryComp<RiderComponent>(args.User, out var rider) && rider?.Vehicle?.Owner != component.Owner)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    var key = EntityManager.SpawnEntity(component.Key, Transform(args.User).Coordinates);
                    _handsSystem.PickupOrDrop(args.User, key);
                    component.HasKey = false;
                    _ambientSound.SetAmbience(uid, false);
                },
                Text = Loc.GetString("vehicle-remove-keys-verb"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        private void OnMove(EntityUid uid, VehicleComponent component, ref MoveEvent args)
        {
            if (!HasComp<SharedPlayerInputMoverComponent>(uid))
                return;
            if ((!component.HasRider || !component.HasKey) && _random.Prob(0.015f))
            {
                RemComp<SharedPlayerInputMoverComponent>(uid);
                UpdateAutoAnimate(uid, false);
            }
            UpdateBuckleOffset(args.Component, component);
            UpdateAppearance(uid, GetDrawDepth(args.Component, component.NorthOnly));
        }

        private void OnStorageChanged(EntityUid uid, VehicleComponent component, StorageChangedEvent args)
        {
            UpdateStorageUsed(uid, args.Added);
        }

        private void OnVirtualItemDeleted(EntityUid uid, RiderComponent component, VirtualItemDeletedEvent args)
        {
            if (args.BlockingEntity == component.Vehicle?.Owner)
            {
                if (!TryComp<BuckleComponent>(uid, out var buckle))
                    return;

                buckle.TryUnbuckle(uid, true);
            }
        }

        private void OnParalyzed(EntityUid uid, RiderComponent rider, GotParalyzedEvent args)
        {
            if (!TryComp<BuckleComponent>(uid, out var buckle))
                return;

            buckle.TryUnbuckle(uid, true);
        }

        private void OnMobStateChanged(EntityUid uid, RiderComponent rider, MobStateChangedEvent args)
        {
            if (!TryComp<BuckleComponent>(uid, out var buckle))
                return;

            if (args.Component.IsCritical() || args.Component.IsDead())
            {
                buckle.TryUnbuckle(uid, true);
            }
        }

        private void OnHonk(EntityUid uid, VehicleComponent vehicle, HonkActionEvent args)
        {
            if (vehicle.HornSound != null)
                SoundSystem.Play(Filter.Pvs(uid), vehicle.HornSound.GetSound(), uid, AudioHelpers.WithVariation(0.1f).WithVolume(8f));
        }
        private int GetDrawDepth(TransformComponent xform, bool northOnly)
        {
            if (northOnly)
            {
                int norfDrawDepth = xform.LocalRotation.Degrees switch
                {
                < 135f => 10,
                <= 225f => 2,
                _ => 10
                };

            return norfDrawDepth;
            }
            int drawDepth = xform.LocalRotation.Degrees switch
            {
              < 45f => 10,
              <= 315f => 2,
              _ => 10
            };

            return drawDepth;
        }

        private void UpdateBuckleOffset(TransformComponent xform, VehicleComponent component)
        {
            var strap = Comp<StrapComponent>(component.Owner);
            strap.BuckleOffsetUnclamped = xform.LocalRotation.Degrees switch
            {
              < 45f => (0, component.SouthOverride),
              <= 135f => component.BaseBuckleOffset,
              < 225f  => (0, component.NorthOverride),
              <= 315f => (component.BaseBuckleOffset.X * -1, component.BaseBuckleOffset.Y),
              _ => (0, component.SouthOverride)
            };

            foreach (var buckledEntity in strap.BuckledEntities)
            {
                var buckleXform = Transform(buckledEntity);
                buckleXform.LocalPosition = strap.BuckleOffset;
            }

        }

        private void UpdateAppearance(EntityUid uid, int drawDepth)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            appearance.SetData(VehicleVisuals.DrawDepth, drawDepth);
        }
        private void UpdateAutoAnimate(EntityUid uid, bool autoAnimate)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;
            appearance.SetData(VehicleVisuals.AutoAnimate, autoAnimate);
        }

        private void UpdateStorageUsed(EntityUid uid, bool storageUsed)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;
            appearance.SetData(VehicleVisuals.StorageUsed, storageUsed);
        }
    }
}
