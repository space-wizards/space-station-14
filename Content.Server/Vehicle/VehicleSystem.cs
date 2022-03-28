using Content.Shared.Vehicle.Components;
using Content.Shared.Buckle.Components;

namespace Content.Server.Vehicle
{
    public sealed class VehicleSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VehicleComponent, BuckleChangeEvent>(OnBuckleChange);
        }
        /// <summary>
        /// Give the user the rider component if they're buckling to the vehicle,
        /// otherwise remove it.
        /// </summary>
        public void OnBuckleChange(EntityUid uid, VehicleComponent component, BuckleChangeEvent args)
        {
            if (args.Buckling)
            {
                var rider = EnsureComp<RiderComponent>(args.BuckledEntity);
                rider.Vehicle = component;
                return;
            }
            RemComp<RiderComponent>(args.BuckledEntity);
        }
    }
}
