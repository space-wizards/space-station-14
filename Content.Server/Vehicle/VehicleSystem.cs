using Content.Shared.Vehicle.Components;
using Content.Shared.Vehicle;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.Pulling.Components;
using Content.Server.Light.Components;
using Content.Server.Buckle.Components;
using Content.Server.Hands.Systems;
using Content.Shared.Tag;
using Robust.Shared.Random;
using Robust.Shared.Containers;

namespace Content.Server.Vehicle
{
    public sealed class VehicleSystem : EntitySystem
    {
        [Dependency] private readonly HandVirtualItemSystem _virtualItemSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly RiderSystem _riderSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VehicleComponent, BuckleChangeEvent>(OnBuckleChange);
            SubscribeLocalEvent<VehicleComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<VehicleComponent, MoveEvent>(OnMove);
            SubscribeLocalEvent<VehicleComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
            SubscribeLocalEvent<VehicleComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
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
        /// <summary>
        /// Sets the initial appearance / sound, then stores the initial buckle offset and resets it.
        /// </summary>
        private void OnComponentInit(EntityUid uid, VehicleComponent component, ComponentInit args)
        {
            UpdateDrawDepth(uid, 2);
            _ambientSound.SetAmbience(uid, false);
            if (!TryComp<StrapComponent>(uid, out var strap))
                return;

            component.BaseBuckleOffset = strap.BuckleOffset;
            strap.BuckleOffsetUnclamped = Vector2.Zero; //You're going to align these facing east, so...
        }
        /// <summary>
        /// Give the user the rider component if they're buckling to the vehicle,
        /// otherwise remove it.
        /// </summary>
        private void OnBuckleChange(EntityUid uid, VehicleComponent component, BuckleChangeEvent args)
        {
            if (args.Buckling)
            {
                // Add a virtual item to rider's hand, unbuckle if we can't.
                if (!_virtualItemSystem.TrySpawnVirtualItemInHand(uid, args.BuckledEntity))
                {
                    _riderSystem.UnbuckleFromVehicle(args.BuckledEntity);
                    return;
                }
                // Set up the rider and vehicle with each other
                EnsureComp<SharedPlayerInputMoverComponent>(uid);
                var rider = EnsureComp<RiderComponent>(args.BuckledEntity);
                component.Rider = args.BuckledEntity;
                rider.Vehicle = component;
                component.HasRider = true;

                // Handle pulling
                RemComp<SharedPullableComponent>(args.BuckledEntity);
                RemComp<SharedPullableComponent>(uid);

                // Let this open doors if it has the key in it
                if (component.HasKey)
                {
                    _tagSystem.AddTag(uid, "DoorBumpOpener");
                }
                // Update appearance stuff, add actions
                UpdateBuckleOffset(Transform(uid), component);
                UpdateDrawDepth(uid, GetDrawDepth(Transform(uid), component.NorthOnly));
                if (TryComp<ActionsComponent>(args.BuckledEntity, out var actions) && TryComp<UnpoweredFlashlightComponent>(uid, out var flashlight))
                {
                    _actionsSystem.AddAction(args.BuckledEntity, flashlight.ToggleAction, uid, actions);
                }
                if (component.HornSound != null)
                {
                    _actionsSystem.AddAction(args.BuckledEntity, component.HornAction, uid, actions);
                }
                _itemSlotsSystem.SetLock(uid, component.Name, true);
                return;
            }
            // Clean up actions and virtual items
            _actionsSystem.RemoveProvidedActions(args.BuckledEntity, uid);
            _virtualItemSystem.DeleteInHandsMatching(args.BuckledEntity, uid);
            // Go back to old pullable behavior
            _tagSystem.RemoveTag(uid, "DoorBumpOpener");
            EnsureComp<SharedPullableComponent>(args.BuckledEntity);
            EnsureComp<SharedPullableComponent>(uid);
            // Entity is no longer riding
            RemComp<RiderComponent>(args.BuckledEntity);
            // Reset component
            component.HasRider = false;
            component.Rider = null;
            _itemSlotsSystem.SetLock(uid, component.Name, false);

        }

        /// <summary>
        /// Every time the vehicle moves we update its visual and buckle positions.
        /// Not the most beautiful thing but it works.
        /// </summary>
        private void OnMove(EntityUid uid, VehicleComponent component, ref MoveEvent args)
        {
            // This first check is just for safety
            if (!HasComp<SharedPlayerInputMoverComponent>(uid))
            {
                UpdateAutoAnimate(uid, false);
                return;
            }
            // The random check means the vehicle will stop after a few tiles without a key or without a rider
            if ((!component.HasRider || !component.HasKey) && _random.Prob(0.015f))
            {
                RemComp<SharedPlayerInputMoverComponent>(uid);
                UpdateAutoAnimate(uid, false);
            }
            UpdateBuckleOffset(args.Component, component);
            UpdateDrawDepth(uid, GetDrawDepth(args.Component, component.NorthOnly));
        }

        /// <summary>
        /// Handle adding keys to the ignition, give stuff the InVehicleComponent so it can't be picked
        /// up by people not in the vehicle.
        /// </summary>
        private void OnEntInserted(EntityUid uid, VehicleComponent component, EntInsertedIntoContainerMessage args)
        {
            var inVehicle = AddComp<InVehicleComponent>(args.Entity);
            inVehicle.Vehicle = component;

            if (_tagSystem.HasTag(args.Entity, "VehicleKey"))
            {
                // Return if the slot is not the key slot
                // That slot ID should be inherited from basevehicle in the .yml
                if (args.Container.ID != "key_slot")
                {
                    return;
                }

                // This lets the vehicle move
                EnsureComp<SharedPlayerInputMoverComponent>(uid);
                // This lets the vehicle open doors
                if (component.HasRider)
                    _tagSystem.AddTag(uid, "DoorBumpOpener");

                component.HasKey = true;

                // Audiovisual feedback
                _ambientSound.SetAmbience(uid, true);
            }
        }

        /// <summary>
        /// Turn off the engine when key is removed.
        /// </summary>
        private void OnEntRemoved(EntityUid uid, VehicleComponent component, EntRemovedFromContainerMessage args)
        {
            RemComp<InVehicleComponent>(args.Entity);

            if (_tagSystem.HasTag(args.Entity, "VehicleKey"))
            {
                component.HasKey = false;
                _ambientSound.SetAmbience(uid, false);
            }
        }

        /// <summary>
        /// Depending on which direction the vehicle is facing,
        /// change its draw depth. Vehicles can choose between special drawdetph
        /// when facing north or south. East and west are easy.
        /// </summary>
        private int GetDrawDepth(TransformComponent xform, bool northOnly)
        {
            if (northOnly)
            {
                return xform.LocalRotation.Degrees switch
                {
                    < 135f => 10,
                    <= 225f => 2,
                    _ => 10
                };
            }
            return xform.LocalRotation.Degrees switch
            {
                < 45f => 10,
                <= 315f => 2,
                _ => 10
            };
        }

        /// <summary>
        /// Change the buckle offset based on what direction the vehicle is facing and
        /// teleport any buckled entities to it. This is the most crucial part of making
        /// buckled vehicles work.
        /// </summary>
        private void UpdateBuckleOffset(TransformComponent xform, VehicleComponent component)
        {
            if (!TryComp<StrapComponent>(component.Owner, out var strap))
                return;
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

        /// <summary>
        /// Set the draw depth for the sprite.
        /// </summary>
        private void UpdateDrawDepth(EntityUid uid, int drawDepth)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            appearance.SetData(VehicleVisuals.DrawDepth, drawDepth);
        }

        /// <summary>
        /// Set whether the vehicle's base layer is animating or not.
        /// </summary>
        private void UpdateAutoAnimate(EntityUid uid, bool autoAnimate)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;
            appearance.SetData(VehicleVisuals.AutoAnimate, autoAnimate);
        }
    }
}
