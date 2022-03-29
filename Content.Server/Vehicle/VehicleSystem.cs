using Content.Shared.Vehicle.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Content.Server.Hands.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Server.Vehicle
{
    public sealed class VehicleSystem : EntitySystem
    {
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VehicleComponent, BuckleChangeEvent>(OnBuckleChange);
            SubscribeLocalEvent<VehicleComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<VehicleComponent, GetVerbsEvent<AlternativeVerb>>(AddKeysVerb);
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
        private void AddKeysVerb(EntityUid uid, VehicleComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess || !HasComp<HandsComponent>(args.User) || !component.HasKey || component.Key == string.Empty)
                return;
            // If someone is riding, let only that person take out the keys.
            if (component.HasRider && !TryComp<RiderComponent>(args.User, out var rider) && rider?.Vehicle?.Owner != component.Owner)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    var key = EntityManager.SpawnEntity(component.Key, Transform(args.User).Coordinates);
                    _handsSystem.PickupOrDrop(args.User, key);
                    component.HasKey = false;
                },
                Text = Loc.GetString("vehicle-remove-keys-verb"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }
    }
}
