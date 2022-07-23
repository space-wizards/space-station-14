using Content.Client.Buckle.Strap;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameStates;

namespace Content.Client.Vehicle
{
    public sealed class VehicleSystem : SharedVehicleSystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RiderComponent, ComponentShutdown>(OnRiderShutdown);
            SubscribeLocalEvent<RiderComponent, ComponentHandleState>(OnRiderHandleState);
            SubscribeLocalEvent<RiderComponent, PlayerAttachedEvent>(OnRiderAttached);
            SubscribeLocalEvent<RiderComponent, PlayerDetachedEvent>(OnRiderDetached);
        }

        private void OnRiderShutdown(EntityUid uid, RiderComponent component, ComponentShutdown args)
        {
            component.Vehicle = null;
            UpdateEye(component);
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
            if (!TryComp(component.Vehicle, out EyeComponent? eyeComponent))
            {
                TryComp(_playerManager.LocalPlayer?.ControlledEntity, out eyeComponent);
            }

            if (eyeComponent?.Eye == null) return;

            _eyeManager.CurrentEye = eyeComponent.Eye;
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
