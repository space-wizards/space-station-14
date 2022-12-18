using Content.Server.Buckle.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Light.Components;
using Content.Shared.Actions;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

namespace Content.Server.Vehicle
{
    public sealed partial class VehicleSystem : SharedVehicleSystem
    {
        [Dependency] private readonly BuckleSystem _buckle = default!;
        [Dependency] private readonly HandVirtualItemSystem _virtualItemSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly SharedJointSystem _joints = default!;
        [Dependency] private readonly SharedMoverController _mover = default!;

        public override void Initialize()
        {
            base.Initialize();

            InitializeRider();

            SubscribeLocalEvent<VehicleComponent, HonkActionEvent>(OnHonk);
            SubscribeLocalEvent<VehicleComponent, BuckleChangeEvent>(OnBuckleChange);
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
                _mover.SetRelay(args.BuckledEntity, uid, relay);
                rider.Vehicle = uid;

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

                _joints.ClearJoints(args.BuckledEntity);

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
            component.Rider = null;
        }
    }
}
