using Content.Shared.Vehicle.Components;
using Content.Shared.Vehicle;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Toggleable;
using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.Pulling.Components;
using Content.Server.Light.Components;
using Content.Server.Buckle.Components;
using Content.Server.Storage.EntitySystems;
using Content.Server.Storage.Components;
using Content.Server.Popups;
using Content.Shared.MobState;
using Content.Shared.Stunnable;
using Content.Server.Hands.Systems;
using Content.Shared.Hands;
using Content.Shared.Tag;
using Robust.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Player;
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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VehicleComponent, HonkActionEvent>(OnHonk);
            SubscribeLocalEvent<VehicleComponent, ToggleActionEvent>(OnSirenToggle);
            SubscribeLocalEvent<VehicleComponent, BuckleChangeEvent>(OnBuckleChange);
            SubscribeLocalEvent<VehicleComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<VehicleComponent, MoveEvent>(OnMove);
            SubscribeLocalEvent<VehicleComponent, StorageChangedEvent>(OnStorageChanged);
            SubscribeLocalEvent<VehicleComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
            SubscribeLocalEvent<VehicleComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
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
        /// <summary>
        /// Sets the initial appearance / sound, then stores the initial buckle offset and resets it.
        /// </summary>
        private void OnComponentInit(EntityUid uid, VehicleComponent component, ComponentInit args)
        {
            UpdateAppearance(uid, 2);
            if (HasComp<ServerStorageComponent>(uid))
                UpdateStorageUsed(uid, false);
            _ambientSound.SetAmbience(uid, false);
            if (!TryComp<StrapComponent>(uid, out var strap))
                return;

            component.BaseBuckleOffset = strap.BuckleOffset;
            strap.BuckleOffsetUnclamped = Vector2.Zero; //You're going to align these facing east, so...

            // Add key slot
            component.KeySlot.Whitelist = component.KeyWhitelist;
            _itemSlotsSystem.AddItemSlot(uid, component.Name, component.KeySlot);
        }
        /// <summary>
        /// Give the user the rider component if they're buckling to the vehicle,
        /// otherwise remove it.
        /// </summary>
        private void OnBuckleChange(EntityUid uid, VehicleComponent component, BuckleChangeEvent args)
        {
            if (args.Buckling)
            {
                /// Set up the rider and vehicle with each other
                EnsureComp<SharedPlayerInputMoverComponent>(uid);
                var rider = EnsureComp<RiderComponent>(args.BuckledEntity);
                component.Rider = args.BuckledEntity;
                rider.Vehicle = component;
                component.HasRider = true;

                /// Handle pulling
                RemComp<SharedPullableComponent>(args.BuckledEntity);
                RemComp<SharedPullableComponent>(uid);
                /// Add a virtual item to rider's hand
                _virtualItemSystem.TrySpawnVirtualItemInHand(uid, args.BuckledEntity);
                /// Let this open doors if it has the key in it
                if (component.KeySlot.HasItem)
                {
                    _tagSystem.AddTag(uid, "DoorBumpOpener");
                }
                /// Update appearance stuff, add actions
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
            /// Entity is no longer riding
            RemComp<RiderComponent>(args.BuckledEntity);
            /// Reset component
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
            /// This first check is just for safety
            if (!HasComp<SharedPlayerInputMoverComponent>(uid))
            {
                UpdateAutoAnimate(uid, false);
                return;
            }
            /// The random check means the vehicle will stop after a few tiles without a key or without a rider
            if ((!component.HasRider || !component.KeySlot.HasItem) && _random.Prob(0.015f))
            {
                RemComp<SharedPlayerInputMoverComponent>(uid);
                UpdateAutoAnimate(uid, false);
            }
            UpdateBuckleOffset(args.Component, component);
            UpdateAppearance(uid, GetDrawDepth(args.Component, component.NorthOnly));
        }

        /// <summary>
        /// This is used for the janicart having its bag inserted / removed
        /// </summary>
        private void OnStorageChanged(EntityUid uid, VehicleComponent component, StorageChangedEvent args)
        {
            UpdateStorageUsed(uid, args.Added);
        }

        /// <summary>
        /// Handle adding keys to the ignition
        /// </summary>
        private void OnEntInserted(EntityUid uid, VehicleComponent component, EntInsertedIntoContainerMessage args)
        {
            if (!_tagSystem.HasTag(args.Entity, "VehicleKey"))
                return;

            /// This lets the vehicle move
            EnsureComp<SharedPlayerInputMoverComponent>(uid);
            /// This lets the vehicle open doors
            if (component.HasRider)
                _tagSystem.AddTag(uid, "DoorBumpOpener");

            // Audiovisual feedback
            SoundSystem.Play(Filter.Pvs(uid), component.StartupSound.GetSound(), uid, AudioParams.Default.WithVolume(1f));
            _ambientSound.SetAmbience(uid, true);
        }

        /// <summary>
        /// Turn off the engine when key is removed.
        /// </summary>
        private void OnEntRemoved(EntityUid uid, VehicleComponent component, EntRemovedFromContainerMessage args)
        {
            /// We have 3 containers or maybe even more so
            if (!_tagSystem.HasTag(args.Entity, "VehicleKey"))
                return;

            _ambientSound.SetAmbience(uid, false);
        }

        /// <summary>
        /// Kick the rider off the vehicle if they press q / drop the virtual item
        /// </summary>
        private void OnVirtualItemDeleted(EntityUid uid, RiderComponent component, VirtualItemDeletedEvent args)
        {
            if (args.BlockingEntity == component.Vehicle?.Owner)
            {
                if (!TryComp<BuckleComponent>(uid, out var buckle))
                    return;

                buckle.TryUnbuckle(uid, true);
            }
        }

        /// <summary>
        /// Kick the rider off the vehicle if they get stunned
        /// </summary>
        private void OnParalyzed(EntityUid uid, RiderComponent rider, GotParalyzedEvent args)
        {
            if (!TryComp<BuckleComponent>(uid, out var buckle))
                return;

            buckle.TryUnbuckle(uid, true);
        }

        /// <summary>
        /// Kick the rider off the vehicle if they go into crit or die.
        /// </summary>
        private void OnMobStateChanged(EntityUid uid, RiderComponent rider, MobStateChangedEvent args)
        {
            if (!TryComp<BuckleComponent>(uid, out var buckle))
                return;

            if (args.Component.IsCritical() || args.Component.IsDead())
            {
                buckle.TryUnbuckle(uid, true);
            }
        }

        /// <summary>
        /// This fires when the rider presses the honk action
        /// </summary>
        private void OnHonk(EntityUid uid, VehicleComponent vehicle, HonkActionEvent args)
        {
            if (args.Handled)
                return;
            if (vehicle.HornSound != null)
            {
                SoundSystem.Play(Filter.Pvs(uid), vehicle.HornSound.GetSound(), uid, AudioHelpers.WithVariation(0.1f).WithVolume(8f));
                args.Handled = true;
            }
        }

        /// <summary>
        /// For vehicles with horn sirens (like the secway) this uses different logic that makes the siren
        /// loop instead of using a normal honk.
        /// </summary>
        private void OnSirenToggle(EntityUid uid, VehicleComponent vehicle, ToggleActionEvent args)
        {
            if (args.Handled || !vehicle.HornIsSiren)
                return;

            if (!vehicle.SirenPlaying)
            {
                vehicle.SirenPlayingStream?.Stop();
                vehicle.SirenPlaying = true;
                if (vehicle.HornSound != null)
                    vehicle.SirenPlayingStream = SoundSystem.Play(Filter.Pvs(uid), vehicle.HornSound.GetSound(), uid, AudioParams.Default.WithLoop(true).WithVolume(1.8f));
                return;
            }
            vehicle.SirenPlayingStream?.Stop();
            vehicle.SirenPlaying = false;
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
        private void UpdateAppearance(EntityUid uid, int drawDepth)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            appearance.SetData(VehicleVisuals.DrawDepth, drawDepth);
        }

        /// <summary>
        /// Set whether the vehicle's base layer is animating or not.
        /// </summmary>
        private void UpdateAutoAnimate(EntityUid uid, bool autoAnimate)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;
            appearance.SetData(VehicleVisuals.AutoAnimate, autoAnimate);
        }

        /// <summary>
        /// Toggle visibility of e.g. the trash bag on the janicart
        /// </summary>
        private void UpdateStorageUsed(EntityUid uid, bool storageUsed)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;
            appearance.SetData(VehicleVisuals.StorageUsed, storageUsed);
        }
    }
}
