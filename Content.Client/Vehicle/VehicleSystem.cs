using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Vehicle
{
    public sealed class VehicleSystem : SharedVehicleSystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RiderComponent, ComponentStartup>(OnRiderStartup);
            SubscribeLocalEvent<RiderComponent, ComponentShutdown>(OnRiderShutdown);
            SubscribeLocalEvent<RiderComponent, ComponentHandleState>(OnRiderHandleState);
        }

        private void OnRiderStartup(EntityUid uid, RiderComponent component, ComponentStartup args)
        {
            // Center the player's eye on the vehicle
            if (TryComp(uid, out EyeComponent? eyeComp))
                eyeComp.Target ??= component.Vehicle;
        }

        private void OnRiderShutdown(EntityUid uid, RiderComponent component, ComponentShutdown args)
        {
            // reset the riders eye centering.
            if (TryComp(uid, out EyeComponent? eyeComp) && eyeComp.Target == component.Vehicle)
                eyeComp.Target = null;
        }

        private void OnRiderHandleState(EntityUid uid, RiderComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not RiderComponentState state)
                return;

            if (TryComp(uid, out EyeComponent? eyeComp) && eyeComp.Target == component.Vehicle)
                eyeComp.Target = state.Entity;

            component.Vehicle = state.Entity;
        }
    }
}
