using Content.Server.Buckle.Components;
using Content.Shared.Vehicle.Components;
using Content.Shared.Vehicle;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Server.Light.Components;
using Content.Server.Hands.Systems;
using Content.Shared.Tag;
using Content.Shared.Movement.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Server.Vehicle
{
    public sealed partial class VehicleSystem : SharedVehicleSystem
    {
        [Dependency] private readonly HandVirtualItemSystem _virtualItemSystem = default!;
        [Dependency] private readonly MetaDataSystem _metadata = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _modifier = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
        [Dependency] private readonly SharedMoverController _mover = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;

        private const string KeySlot = "key_slot";

        public override void Initialize()
        {
            base.Initialize();

            InitializeRider();

            SubscribeLocalEvent<VehicleComponent, HonkActionEvent>(OnHonk);
            SubscribeLocalEvent<VehicleComponent, BuckleChangeEvent>(OnBuckleChange);
            SubscribeLocalEvent<VehicleComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
            SubscribeLocalEvent<VehicleComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        }

        /// <summary>
        /// This fires when the rider presses the honk action
        /// </summary>
        private void OnHonk(EntityUid uid, VehicleComponent vehicle, HonkActionEvent args)
        {
            if (args.Handled || vehicle.HornSound == null)
                return;

            // TODO: Need audio refactor maybe, just some way to null it when the stream is over.
            // For now better to just not loop to keep the code much cleaner.
            vehicle.HonkPlayingStream?.Stop();
            vehicle.HonkPlayingStream = SoundSystem.Play(vehicle.HornSound.GetSound(), Filter.Pvs(uid), uid, vehicle.HornSound.Params);
            args.Handled = true;
        }

        /// <summary>
        /// This just controls whether the wheels are turning.
        /// </summary>
        public override void Update(float frameTime)
        {
            foreach (var (vehicle, mover) in EntityQuery<VehicleComponent, InputMoverComponent>())
            {
                if (_mover.GetVelocityInput(mover).Sprinting == Vector2.Zero)
                {
                    UpdateAutoAnimate(vehicle.Owner, false);
                    continue;
                }
                UpdateAutoAnimate(vehicle.Owner, true);
            }
        }

        /// <summary>
        /// Give the user the rider component if they're buckling to the vehicle,
        /// otherwise remove it.
        /// </summary>
        private void OnBuckleChange(EntityUid uid, VehicleComponent component, BuckleChangeEvent args)
        {
            // Add Rider
            if (args.Buckling)
            {
                // Add a virtual item to rider's hand, unbuckle if we can't.
                if (!_virtualItemSystem.TrySpawnVirtualItemInHand(uid, args.BuckledEntity))
                {
                    UnbuckleFromVehicle(args.BuckledEntity);
                    return;
                }

                // Set up the rider and vehicle with each other
                EnsureComp<InputMoverComponent>(uid);
                var rider = EnsureComp<RiderComponent>(args.BuckledEntity);
                component.Rider = args.BuckledEntity;

                var relay = EnsureComp<RelayInputMoverComponent>(args.BuckledEntity);
                relay.RelayEntity = uid;
                rider.Vehicle = uid;
                component.HasRider = true;

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

                return;
            }

            // Remove rider

            // Clean up actions and virtual items
            _actionsSystem.RemoveProvidedActions(args.BuckledEntity, uid);
            _virtualItemSystem.DeleteInHandsMatching(args.BuckledEntity, uid);

            // Entity is no longer riding
            RemComp<RiderComponent>(args.BuckledEntity);
            RemComp<RelayInputMoverComponent>(args.BuckledEntity);

            // Reset component
            component.HasRider = false;
            component.Rider = null;
        }

        /// <summary>
        /// Handle adding keys to the ignition, give stuff the InVehicleComponent so it can't be picked
        /// up by people not in the vehicle.
        /// </summary>
        private void OnEntInserted(EntityUid uid, VehicleComponent component, EntInsertedIntoContainerMessage args)
        {
            if (args.Container.ID != KeySlot ||
                !_tagSystem.HasTag(args.Entity, "VehicleKey")) return;

            // Enable vehicle
            var inVehicle = AddComp<InVehicleComponent>(args.Entity);
            inVehicle.Vehicle = component;

            component.HasKey = true;

            // Audiovisual feedback
            _ambientSound.SetAmbience(uid, true);
            _tagSystem.AddTag(uid, "DoorBumpOpener");
            _modifier.RefreshMovementSpeedModifiers(uid);
        }

        /// <summary>
        /// Turn off the engine when key is removed.
        /// </summary>
        private void OnEntRemoved(EntityUid uid, VehicleComponent component, EntRemovedFromContainerMessage args)
        {
            if (args.Container.ID != KeySlot || !RemComp<InVehicleComponent>(args.Entity)) return;

            // Disable vehicle
            component.HasKey = false;
            _ambientSound.SetAmbience(uid, false);
            _tagSystem.RemoveTag(uid, "DoorBumpOpener");
            _modifier.RefreshMovementSpeedModifiers(uid);
        }
    }
}
