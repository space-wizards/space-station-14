using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;

namespace Content.Client.Vehicle
{
    public sealed class VehicleSystem : SharedVehicleSystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RiderComponent, ComponentHandleState>(OnRiderHandleState);
            SubscribeLocalEvent<RiderComponent, PlayerAttachedEvent>(OnRiderAttached);
            SubscribeLocalEvent<RiderComponent, PlayerDetachedEvent>(OnRiderDetached);
        }

        private void OnRiderAttached(EntityUid uid, RiderComponent component, PlayerAttachedEvent args)
        {
            UpdateEye(component);
        }

        private void OnRiderDetached(EntityUid uid, RiderComponent component, PlayerDetachedEvent args)
        {
            UpdateEye(component);
        }

        private void UpdateEye(RiderComponent component)
        {
            if (!TryComp<EyeComponent>(component.Vehicle, out var vehicleEye) || vehicleEye.Eye == null) return;

            _eyeManager.CurrentEye = vehicleEye.Eye;
        }

        private void OnRiderHandleState(EntityUid uid, RiderComponent component, ref ComponentHandleState args)
        {
            // Server should only be sending states for our entity.
            if (args.Current is not RiderComponentState state) return;
            component.Vehicle = state.Entity;

            UpdateEye(component);
        }
    }
}
