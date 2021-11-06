using System;
using Content.Server.Alert;
using Content.Server.Atmos.Components;
using Content.Server.Stunnable;
using Content.Server.Stunnable.Components;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Temperature;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Atmos.EntitySystems
{
    internal sealed class FlammableSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly TemperatureSystem _temperatureSystem = default!;

        private const float MinimumFireStacks = -10f;
        private const float MaximumFireStacks = 20f;
        private const float UpdateTime = 1f;

        private float _timer = 0f;

        // TODO: Port the rest of Flammable.
        public override void Initialize()
        {
            SubscribeLocalEvent<FlammableComponent, InteractUsingEvent>(OnInteractUsingEvent);
            SubscribeLocalEvent<FlammableComponent, StartCollideEvent>(OnCollideEvent);
            SubscribeLocalEvent<FlammableComponent, IsHotEvent>(OnIsHotEvent);
            SubscribeLocalEvent<FlammableComponent, TileFireEvent>(OnTileFireEvent);
        }

        private void OnInteractUsingEvent(EntityUid uid, FlammableComponent flammable, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(args.Used.Uid, isHotEvent, false);

            if (!isHotEvent.IsHot)
                return;

            Ignite(uid, flammable);
            args.Handled = true;
        }

        private void OnCollideEvent(EntityUid uid, FlammableComponent flammable, StartCollideEvent args)
        {
            var otherUid = args.OtherFixture.Body.Owner.Uid;
            if (!EntityManager.TryGetComponent(otherUid, out FlammableComponent? otherFlammable))
                return;

            if (!flammable.FireSpread || !otherFlammable.FireSpread)
                return;

            if (flammable.OnFire)
            {
                if (otherFlammable.OnFire)
                {
                    var fireSplit = (flammable.FireStacks + otherFlammable.FireStacks) / 2;
                    flammable.FireStacks = fireSplit;
                    otherFlammable.FireStacks = fireSplit;
                }
                else
                {
                    flammable.FireStacks /= 2;
                    otherFlammable.FireStacks += flammable.FireStacks;
                    Ignite(otherUid, otherFlammable);
                }
            } else if (otherFlammable.OnFire)
            {
                otherFlammable.FireStacks /= 2;
                flammable.FireStacks += otherFlammable.FireStacks;
                Ignite(uid, flammable);
            }
        }

        private void OnIsHotEvent(EntityUid uid, FlammableComponent flammable, IsHotEvent args)
        {
            args.IsHot = flammable.OnFire;
        }

        private void OnTileFireEvent(EntityUid uid, FlammableComponent flammable, TileFireEvent args)
        {
            AdjustFireStacks(uid, 3, flammable);
            Ignite(uid, flammable);
        }

        public void UpdateAppearance(EntityUid uid, FlammableComponent? flammable = null, AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref flammable, ref appearance))
                return;

            appearance.SetData(FireVisuals.OnFire, flammable.OnFire);
            appearance.SetData(FireVisuals.FireStacks, flammable.FireStacks);
        }

        public void AdjustFireStacks(EntityUid uid, float relativeFireStacks, FlammableComponent? flammable = null)
        {
            if (!Resolve(uid, ref flammable))
                return;

            flammable.FireStacks = MathF.Min(MathF.Max(MinimumFireStacks, flammable.FireStacks + relativeFireStacks), MaximumFireStacks);

            if (flammable.OnFire && flammable.FireStacks <= 0)
                Extinguish(uid, flammable);

            UpdateAppearance(uid, flammable);
        }

        public void Extinguish(EntityUid uid, FlammableComponent? flammable = null)
        {
            if (!Resolve(uid, ref flammable))
                return;

            if (!flammable.OnFire)
                return;

            flammable.OnFire = false;
            flammable.FireStacks = 0;

            flammable.Collided.Clear();

            UpdateAppearance(uid, flammable);
        }

        public void Ignite(EntityUid uid, FlammableComponent? flammable = null)
        {
            if (!Resolve(uid, ref flammable))
                return;

            if (flammable.FireStacks > 0 && !flammable.OnFire)
            {
                flammable.OnFire = true;
            }

            UpdateAppearance(uid, flammable);
        }

        public void Resist(EntityUid uid,
            FlammableComponent? flammable = null,
            ServerAlertsComponent? alerts = null)
        {
            if (!Resolve(uid, ref flammable, ref alerts))
                return;

            if (!flammable.OnFire || !_actionBlockerSystem.CanInteract(flammable.Owner) || flammable.Resisting)
                return;

            flammable.Resisting = true;

            flammable.Owner.PopupMessage(Loc.GetString("flammable-component-resist-message"));
            _stunSystem.TryParalyze(uid, TimeSpan.FromSeconds(2f), alerts: alerts);

            // TODO FLAMMABLE: Make this not use TimerComponent...
            flammable.Owner.SpawnTimer(2000, () =>
            {
                flammable.Resisting = false;
                flammable.FireStacks -= 3f;
                UpdateAppearance(uid, flammable);
            });
        }

        public override void Update(float frameTime)
        {
            _timer += frameTime;

            if (_timer < UpdateTime)
                return;

            _timer -= UpdateTime;

            // TODO: This needs cleanup to take off the crust from TemperatureComponent and shit.
            foreach (var (flammable, physics, transform) in EntityManager.EntityQuery<FlammableComponent, IPhysBody, ITransformComponent>())
            {
                var uid = flammable.Owner.Uid;

                // Slowly dry ourselves off if wet.
                if (flammable.FireStacks < 0)
                {
                    flammable.FireStacks = MathF.Min(0, flammable.FireStacks + 1);
                }

                flammable.Owner.TryGetComponent(out ServerAlertsComponent? status);

                if (!flammable.OnFire)
                {
                    status?.ClearAlert(AlertType.Fire);
                    continue;
                }

                status?.ShowAlert(AlertType.Fire);

                if (flammable.FireStacks > 0)
                {
                    _temperatureSystem.ReceiveHeat(uid, 200 * flammable.FireStacks);
                    // TODO ATMOS Fire resistance from armor
                    var damageScale = Math.Min((int) (flammable.FireStacks * 2.5f), 10);
                    _damageableSystem.TryChangeDamage(uid, flammable.Damage * damageScale);

                    AdjustFireStacks(uid, -0.1f * (flammable.Resisting ? 10f : 1f), flammable);
                }
                else
                {
                    Extinguish(uid, flammable);
                    continue;
                }

                var air = _atmosphereSystem.GetTileMixture(transform.Coordinates);

                // If we're in an oxygenless environment, put the fire out.
                if (air == null || air.GetMoles(Gas.Oxygen) < 1f)
                {
                    Extinguish(uid, flammable);
                    continue;
                }

                _atmosphereSystem.HotspotExpose(transform.Coordinates, 700f, 50f, true);

                foreach (var otherUid in flammable.Collided.ToArray())
                {
                    if (!otherUid.IsValid() || !EntityManager.EntityExists(otherUid))
                    {
                        flammable.Collided.Remove(otherUid);
                        continue;
                    }

                    var otherPhysics = EntityManager.GetComponent<IPhysBody>(uid);

                    // TODO: Sloth, please save our souls!
                    if (!physics.GetWorldAABB().Intersects(otherPhysics.GetWorldAABB()))
                    {
                        flammable.Collided.Remove(otherUid);
                    }
                }
            }
        }
    }
}
