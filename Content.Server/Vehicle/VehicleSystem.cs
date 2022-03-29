using Content.Shared.Vehicle.Components;
using Content.Shared.Vehicle;
using Content.Shared.Buckle.Components;
using Content.Server.Buckle.Components;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Content.Server.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Server.GameObjects;

namespace Content.Server.Vehicle
{
    public sealed class VehicleSystem : EntitySystem
    {
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VehicleComponent, BuckleChangeEvent>(OnBuckleChange);
            SubscribeLocalEvent<VehicleComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<VehicleComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<VehicleComponent, GetVerbsEvent<AlternativeVerb>>(AddKeysVerb);
            SubscribeLocalEvent<VehicleComponent, MoveEvent>(OnMove);
        }

        public void OnComponentInit(EntityUid uid, VehicleComponent component, ComponentInit args)
        {
            var strap = Comp<StrapComponent>(uid);

            component.BaseBuckleOffset = strap.BuckleOffset;
            strap.BuckleOffsetUnclamped = Vector2.Zero; //You're going to align these facing east, so...
            UpdateAppearance(uid, -10);
        }
        /// <summary>
        /// Give the user the rider component if they're buckling to the vehicle,
        /// otherwise remove it.
        /// </summary>
        public void OnBuckleChange(EntityUid uid, VehicleComponent component, BuckleChangeEvent args)
        {
            TryComp<EyeComponent>(args.BuckledEntity, out var eye);
            if (args.Buckling)
            {
                var rider = EnsureComp<RiderComponent>(args.BuckledEntity);
                rider.Vehicle = component;
                component.HasRider = true;
                UpdateBuckleOffset(Transform(uid), component);
                UpdateAppearance(uid, GetDrawDepth(Transform(uid)));
                if (eye != null)
                    eye.Offset = component.BaseBuckleOffset;
                return;
            }
            RemComp<RiderComponent>(args.BuckledEntity);
            component.HasRider = false;
            if (eye != null)
                eye.Offset = Vector2.Zero;
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

        private void OnMove(EntityUid uid, VehicleComponent component, ref MoveEvent args)
        {
            if (!component.HasRider)
                return;

            UpdateBuckleOffset(args.Component, component);
            UpdateAppearance(uid, GetDrawDepth(args.Component));
        }

        private int GetDrawDepth(TransformComponent xform)
        {
            int drawDepth = xform.LocalRotation.Degrees switch
            {
              < 45f => 10,
              <= 315f => 2,
              _ => 10
            };

            return drawDepth;
        }

        private void UpdateBuckleOffset(TransformComponent xform, VehicleComponent component)
        {
            var strap = Comp<StrapComponent>(component.Owner);
            strap.BuckleOffsetUnclamped = xform.LocalRotation.Degrees switch
            {
              < 45f => (0, component.BaseBuckleOffset.Y),
              <= 135f => component.BaseBuckleOffset,
              < 225f  => (Vector2.Zero),
              <= 315f => (component.BaseBuckleOffset.X * -1, component.BaseBuckleOffset.Y),
              _ => (0, component.BaseBuckleOffset.Y)
            };

            foreach (var buckledEntity in strap.BuckledEntities)
            {
                var buckleXform = Transform(buckledEntity);
                buckleXform.LocalPosition = strap.BuckleOffset;
            }

        }

        private void UpdateAppearance(EntityUid uid, int drawDepth)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            appearance.SetData(VehicleVisuals.DrawDepth, drawDepth);
        }
    }
}
