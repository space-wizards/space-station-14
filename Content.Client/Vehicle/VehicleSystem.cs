using Content.Shared.Vehicle;
using Robust.Client.Graphics;
using Robust.Client.GameObjects;

namespace Content.Client.Vehicle
{
    public sealed class VehicleSystem : EntitySystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<BuckledToVehicleEvent>(OnBuckle);
        }

        private void OnBuckle(BuckledToVehicleEvent args)
        {
            // Use the vehicle's eye if we get buckled
            if (args.Buckling)
            {
                if (!TryComp<EyeComponent>(args.Vehicle, out var vehicleEye) || vehicleEye.Eye == null)
                    return;
                _eyeManager.CurrentEye = vehicleEye.Eye;
                return;
            }
            // Reset if we get unbuckled.
            if (!TryComp<EyeComponent>(args.Rider, out var component) || component.Eye == null)
                return; // This probably will never happen but in this strange new world we probably want to maintain our old vision
            _eyeManager.CurrentEye = component.Eye;
        }

    }
}
