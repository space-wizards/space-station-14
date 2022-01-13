using System;
using System.Collections.Generic;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.Components;
using Content.Server.Stunnable;
using Content.Server.Temperature.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Temperature;
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
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly TemperatureSystem _temperatureSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly AdminLogSystem _logSystem = default!;

        private const float MinimumFireStacks = -10f;
        private const float MaximumFireStacks = 20f;
        private const float UpdateTime = 1f;

        private const float MinIgnitionTemperature = 373.15f;

        private float _timer = 0f;

        private Dictionary<FlammableComponent, float> _fireEvents = new();

        // TODO: Port the rest of Flammable.
        public override void Initialize()
        {
            UpdatesAfter.Add(typeof(AtmosphereSystem));

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
            RaiseLocalEvent(args.Used, isHotEvent, false);

            if (!isHotEvent.IsHot)
                return;

            Ignite(uid, flammable);
            args.Handled = true;
        }

        private void OnCollideEvent(EntityUid uid, FlammableComponent flammable, StartCollideEvent args)
        {
            var otherUid = args.OtherFixture.Body.Owner;
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
            var tempDelta = args.Temperature - MinIgnitionTemperature;

            var maxTemp = 0f;
            _fireEvents.TryGetValue(flammable, out maxTemp);

            if (tempDelta > maxTemp)
                _fireEvents[flammable] = tempDelta;
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

            _logSystem.Add(LogType.Flammable, $"{ToPrettyString(flammable.Owner):entity} stopped being on fire damage");
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
                _logSystem.Add(LogType.Flammable, $"{ToPrettyString(flammable.Owner):entity} is on fire");
                flammable.OnFire = true;
            }

            UpdateAppearance(uid, flammable);
        }

        public void Resist(EntityUid uid,
            FlammableComponent? flammable = null)
        {
            if (!Resolve(uid, ref flammable))
                return;

            if (!flammable.OnFire || !_actionBlockerSystem.CanInteract(flammable.Owner) || flammable.Resisting)
                return;

            flammable.Resisting = true;

            flammable.Owner.PopupMessage(Loc.GetString("flammable-component-resist-message"));
            _stunSystem.TryParalyze(uid, TimeSpan.FromSeconds(2f), true);

            // TODO FLAMMABLE: Make this not use TimerComponent...
            flammable.Owner.SpawnTimer(2000, () =>
            {
                flammable.Resisting = false;
                flammable.FireStacks -= 1f;
                UpdateAppearance(uid, flammable);
            });
        }

        public override void Update(float frameTime)
        {
            // process all fire events
            foreach (var (flammable, deltaTemp) in _fireEvents)
            {
                // 100 -> 1, 200 -> 2, 400 -> 3...
                var fireStackMod = Math.Max(MathF.Log2(deltaTemp / 100) + 1, 0);
                var fireStackDelta = fireStackMod - flammable.FireStacks;
                if (fireStackDelta > 0)
                {
                    AdjustFireStacks((flammable).Owner, fireStackDelta, flammable);
                }
                Ignite((flammable).Owner, flammable);
            }
            _fireEvents.Clear();

            _timer += frameTime;

            if (_timer < UpdateTime)
                return;

            _timer -= UpdateTime;

            // TODO: This needs cleanup to take off the crust from TemperatureComponent and shit.
            foreach (var (flammable, physics, transform) in EntityManager.EntityQuery<FlammableComponent, IPhysBody, TransformComponent>())
            {
                var uid = flammable.Owner;

                // Slowly dry ourselves off if wet.
                if (flammable.FireStacks < 0)
                {
                    flammable.FireStacks = MathF.Min(0, flammable.FireStacks + 1);
                }

                if (!flammable.OnFire)
                {
                    _alertsSystem.ClearAlert(uid, AlertType.Fire);
                    continue;
                }

                _alertsSystem.ShowAlert(uid, AlertType.Fire, null, null);

                if (flammable.FireStacks > 0)
                {
                    // TODO FLAMMABLE: further balancing
                    var damageScale = Math.Min((int)flammable.FireStacks, 5);
                    _temperatureSystem.ChangeHeat(uid, 12500 * damageScale);
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
