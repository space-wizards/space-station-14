using Content.Shared.Vehicle.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Content.Server.Hands.Components;

namespace Content.Server.Vehicle
{
    public sealed class VehicleSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VehicleComponent, BuckleChangeEvent>(OnBuckleChange);
            SubscribeLocalEvent<VehicleComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
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
                component.HasRider = true;
                return;
            }
            RemComp<RiderComponent>(args.BuckledEntity);
            component.HasRider = false;
        }

        /// <summary>
        /// Handle adding keys to the ignition
        /// </summary>
        public void OnAfterInteractUsing(EntityUid uid, VehicleComponent component, AfterInteractUsingEvent args)
        {
            if (!args.CanReach)
                return;

            if (!HasComp<HandsComponent>(args.User))
                return;

            if (!TryComp<VehicleKeyComponent>(args.Used, out var key))
                return;

            var keyProto = MetaData(key.Owner);

            if (keyProto.EntityPrototype?.ID != component.Key)
                return;

            component.HasKey = true;
            EntityManager.DeleteEntity(args.Used);
        }
    }
}
