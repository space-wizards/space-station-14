using Content.Client.Eye;
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
        [Dependency] private readonly EyeLerpingSystem _lerpSys = default!;

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
            if (component.Vehicle != null)
                _lerpSys.RemoveEye(component.Vehicle.Value);

            if (uid == _playerManager.LocalPlayer?.ControlledEntity
                && TryComp(uid, out EyeComponent? eye)
                && eye.Eye != null)
            {
                _eyeManager.CurrentEye = eye.Eye;
            }

            component.Vehicle = null;
        }

        private void OnRiderAttached(EntityUid uid, RiderComponent component, PlayerAttachedEvent args)
        {
            UpdateEye(component);
        }

        private void OnRiderDetached(EntityUid uid, RiderComponent component, PlayerDetachedEvent args)
        {
            if (component.Vehicle != null)
                _lerpSys.RemoveEye(component.Vehicle.Value);
        }

        private void UpdateEye(RiderComponent component)
        {
            if (!TryComp(component.Vehicle, out EyeComponent? eyeComponent) || eyeComponent.Eye == null)
                return;

            _lerpSys.AddEye(component.Vehicle.Value, eyeComponent);
            _eyeManager.CurrentEye = eyeComponent.Eye;
        }

        private void OnRiderHandleState(EntityUid uid, RiderComponent component, ref ComponentHandleState args)
        {
            if (uid != _playerManager.LocalPlayer?.ControlledEntity)
                return;

            if (args.Current is not RiderComponentState state) return;
            component.Vehicle = state.Entity;

            UpdateEye(component);
        }
    }
}
