using Content.Server.Administration.Logs;
using Content.Server.Atmos.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.IgnitionSource;
using Content.Server.Stunnable;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Temperature;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed class FlammableSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly TemperatureSystem _temperatureSystem = default!;
        [Dependency] private readonly IgnitionSourceSystem _ignitionSourceSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly FixtureSystem _fixture = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly UseDelaySystem _useDelay = default!;
        [Dependency] private readonly AudioSystem _audio = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public const float MinimumFireStacks = -10f;
        public const float MaximumFireStacks = 20f;
        private const float UpdateTime = 1f;

        public const float MinIgnitionTemperature = 373.15f;
        public const string FlammableFixtureID = "flammable";

        private float _timer;

        private readonly Dictionary<Entity<FlammableComponent>, float> _fireEvents = new();
        private readonly List<EntityUid> _toRemove = new();

        public override void Initialize()
        {
            UpdatesAfter.Add(typeof(AtmosphereSystem));

            SubscribeLocalEvent<FlammableComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<FlammableComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<FlammableComponent, StartCollideEvent>(OnCollide);
            SubscribeLocalEvent<FlammableComponent, IsHotEvent>(OnIsHot);
            SubscribeLocalEvent<FlammableComponent, TileFireEvent>(OnTileFire);
            SubscribeLocalEvent<FlammableComponent, RejuvenateEvent>(OnRejuvenate);

            SubscribeLocalEvent<IgniteOnCollideComponent, StartCollideEvent>(IgniteOnCollide);
            SubscribeLocalEvent<IgniteOnCollideComponent, LandEvent>(OnIgniteLand);

            SubscribeLocalEvent<IgniteOnMeleeHitComponent, MeleeHitEvent>(OnMeleeHit);

            SubscribeLocalEvent<ExtinguishOnInteractComponent, ActivateInWorldEvent>(OnExtinguishActivateInWorld);
        }

        private void OnMeleeHit(EntityUid uid, IgniteOnMeleeHitComponent component, MeleeHitEvent args)
        {
            foreach (var entity in args.HitEntities)
            {
                if (!TryComp<FlammableComponent>(entity, out var flammable))
                    continue;

                AdjustFireStacks(entity, component.FireStacks, flammable);
                if (component.FireStacks >= 0)
                    Ignite(entity, args.Weapon, flammable, args.User);
            }
        }

        private void OnIgniteLand(EntityUid uid, IgniteOnCollideComponent component, ref LandEvent args)
        {
            RemCompDeferred<IgniteOnCollideComponent>(uid);
        }

        private void IgniteOnCollide(EntityUid uid, IgniteOnCollideComponent component, ref StartCollideEvent args)
        {
            if (!args.OtherFixture.Hard || component.Count == 0)
                return;

            var otherEnt = args.OtherEntity;

            if (!EntityManager.TryGetComponent(otherEnt, out FlammableComponent? flammable))
                return;

            flammable.FireStacks += component.FireStacks;
            Ignite(otherEnt, uid, flammable);
            component.Count--;

            if (component.Count == 0)
                RemCompDeferred<IgniteOnCollideComponent>(uid);
        }

        private void OnMapInit(EntityUid uid, FlammableComponent component, MapInitEvent args)
        {
            // Sets up a fixture for flammable collisions.
            // TODO: Should this be generalized into a general non-hard 'effects' fixture or something? I can't think of other use cases for it.
            // This doesn't seem great either (lots more collisions generated) but there isn't a better way to solve it either that I can think of.

            if (!TryComp<PhysicsComponent>(uid, out var body))
                return;

            _fixture.TryCreateFixture(uid, component.FlammableCollisionShape, FlammableFixtureID, hard: false,
                collisionMask: (int) CollisionGroup.FullTileLayer, body: body);
        }

        private void OnInteractUsing(EntityUid uid, FlammableComponent flammable, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(args.Used, isHotEvent);

            if (!isHotEvent.IsHot)
                return;

            Ignite(uid, args.Used, flammable, args.User);
            args.Handled = true;
        }

        private void OnExtinguishActivateInWorld(EntityUid uid, ExtinguishOnInteractComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp(uid, out FlammableComponent? flammable))
                return;

            if (!flammable.OnFire)
                return;

            args.Handled = true;

            if (!_useDelay.BeginDelay(uid))
                return;

            _audio.PlayPvs(component.ExtinguishAttemptSound, uid);
            if (_random.Prob(component.Probability))
            {
                AdjustFireStacks(uid, component.StackDelta, flammable);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString(component.ExtinguishFailed), uid);
            }
        }
        private void OnCollide(EntityUid uid, FlammableComponent flammable, ref StartCollideEvent args)
        {
            var otherUid = args.OtherEntity;

            // Collisions cause events to get raised directed at both entities. We only want to handle this collision
            // once, hence the uid check.
            if (otherUid.Id < uid.Id)
                return;

            // Normal hard collisions, though this isn't generally possible since most flammable things are mobs
            // which don't collide with one another, shouldn't work here.
            if (args.OtherFixtureId != FlammableFixtureID && args.OurFixtureId != FlammableFixtureID)
                return;

            if (!flammable.FireSpread)
                return;

            if (!TryComp(otherUid, out FlammableComponent? otherFlammable) || !otherFlammable.FireSpread)
                return;

            if (!flammable.OnFire && !otherFlammable.OnFire)
                return; // Neither are on fire

            if (flammable.OnFire && otherFlammable.OnFire)
            {
                // Both are on fire -> equalize fire stacks.
                var avg = (flammable.FireStacks + otherFlammable.FireStacks) / 2;
                flammable.FireStacks = flammable.CanExtinguish ? avg : Math.Max(flammable.FireStacks, avg);
                otherFlammable.FireStacks = otherFlammable.CanExtinguish ? avg : Math.Max(otherFlammable.FireStacks, avg);
                UpdateAppearance(uid, flammable);
                UpdateAppearance(otherUid, otherFlammable);
                return;
            }

            // Only one is on fire -> attempt to spread the fire.
            if (flammable.OnFire)
            {
                otherFlammable.FireStacks += flammable.FireStacks / 2;
                Ignite(otherUid, uid, otherFlammable);
                if (flammable.CanExtinguish)
                {
                    flammable.FireStacks /= 2;
                    UpdateAppearance(uid, flammable);
                }
            }
            else
            {
                flammable.FireStacks += otherFlammable.FireStacks / 2;
                Ignite(uid, otherUid, flammable);
                if (otherFlammable.CanExtinguish)
                {
                    otherFlammable.FireStacks /= 2;
                    UpdateAppearance(otherUid, otherFlammable);
                }
            }
        }

        private void OnIsHot(EntityUid uid, FlammableComponent flammable, IsHotEvent args)
        {
            args.IsHot = flammable.OnFire;
        }

        private void OnTileFire(Entity<FlammableComponent> ent, ref TileFireEvent args)
        {
            var tempDelta = args.Temperature - MinIgnitionTemperature;

            _fireEvents.TryGetValue(ent, out var maxTemp);

            if (tempDelta > maxTemp)
                _fireEvents[ent] = tempDelta;
        }

        private void OnRejuvenate(EntityUid uid, FlammableComponent component, RejuvenateEvent args)
        {
            Extinguish(uid, component);
        }

        public void UpdateAppearance(EntityUid uid, FlammableComponent? flammable = null, AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref flammable, ref appearance))
                return;

            _appearance.SetData(uid, FireVisuals.OnFire, flammable.OnFire, appearance);
            _appearance.SetData(uid, FireVisuals.FireStacks, flammable.FireStacks, appearance);

            // Also enable toggleable-light visuals
            // This is intended so that matches & candles can re-use code for un-shaded layers on in-hand sprites.
            // However, this could cause conflicts if something is ACTUALLY both a toggleable light and flammable.
            // if that ever happens, then fire visuals will need to implement their own in-hand sprite management.
            _appearance.SetData(uid, ToggleableLightVisuals.Enabled, flammable.OnFire, appearance);
        }

        public void AdjustFireStacks(EntityUid uid, float relativeFireStacks, FlammableComponent? flammable = null)
        {
            if (!Resolve(uid, ref flammable))
                return;

            flammable.FireStacks = MathF.Min(MathF.Max(MinimumFireStacks, flammable.FireStacks + relativeFireStacks), MaximumFireStacks);

            if (flammable.OnFire && flammable.FireStacks <= 0)
                Extinguish(uid, flammable);
            else
                UpdateAppearance(uid, flammable);
        }

        public void Extinguish(EntityUid uid, FlammableComponent? flammable = null)
        {
            if (!Resolve(uid, ref flammable))
                return;

            if (!flammable.OnFire || !flammable.CanExtinguish)
                return;

            _adminLogger.Add(LogType.Flammable, $"{ToPrettyString(uid):entity} stopped being on fire damage");
            flammable.OnFire = false;
            flammable.FireStacks = 0;

            _ignitionSourceSystem.SetIgnited(uid, false);

            UpdateAppearance(uid, flammable);
        }

        public void Ignite(EntityUid uid, EntityUid ignitionSource, FlammableComponent? flammable = null,
            EntityUid? ignitionSourceUser = null)
        {
            if (!Resolve(uid, ref flammable))
                return;

            if (flammable.AlwaysCombustible)
            {
                flammable.FireStacks = Math.Max(flammable.FirestacksOnIgnite, flammable.FireStacks);
            }

            if (flammable.FireStacks > 0 && !flammable.OnFire)
            {
                if (ignitionSourceUser != null)
                    _adminLogger.Add(LogType.Flammable, $"{ToPrettyString(uid):target} set on fire by {ToPrettyString(ignitionSourceUser.Value):actor} with {ToPrettyString(ignitionSource):tool}");
                else
                    _adminLogger.Add(LogType.Flammable, $"{ToPrettyString(uid):target} set on fire by {ToPrettyString(ignitionSource):actor}");
                flammable.OnFire = true;
            }

            UpdateAppearance(uid, flammable);
        }

        public void Resist(EntityUid uid,
            FlammableComponent? flammable = null)
        {
            if (!Resolve(uid, ref flammable))
                return;

            if (!flammable.OnFire || !_actionBlockerSystem.CanInteract(uid, null) || flammable.Resisting)
                return;

            flammable.Resisting = true;

            _popup.PopupEntity(Loc.GetString("flammable-component-resist-message"), uid, uid);
            _stunSystem.TryParalyze(uid, TimeSpan.FromSeconds(2f), true);

            // TODO FLAMMABLE: Make this not use TimerComponent...
            uid.SpawnTimer(2000, () =>
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
                var fireStackDelta = fireStackMod - flammable.Comp.FireStacks;
                var flammableEntity = flammable.Owner;
                if (fireStackDelta > 0)
                {
                    AdjustFireStacks(flammableEntity, fireStackDelta, flammable);
                }
                Ignite(flammableEntity, flammableEntity, flammable);
            }
            _fireEvents.Clear();

            _timer += frameTime;

            if (_timer < UpdateTime)
                return;

            _timer -= UpdateTime;

            // TODO: This needs cleanup to take off the crust from TemperatureComponent and shit.
            var query = EntityQueryEnumerator<FlammableComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var flammable, out var transform))
            {
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

                _alertsSystem.ShowAlert(uid, AlertType.Fire);

                if (flammable.FireStacks > 0)
                {
                    // TODO FLAMMABLE: further balancing
                    var damageScale = Math.Min((int)flammable.FireStacks, 5);

                    if(TryComp(uid, out TemperatureComponent? temp))
                        _temperatureSystem.ChangeHeat(uid, 12500 * damageScale, false, temp);

                    _damageableSystem.TryChangeDamage(uid, flammable.Damage * damageScale);

                    AdjustFireStacks(uid, flammable.FirestackFade * (flammable.Resisting ? 10f : 1f), flammable);
                }
                else
                {
                    Extinguish(uid, flammable);
                    continue;
                }

                var air = _atmosphereSystem.GetContainingMixture(uid);

                // If we're in an oxygenless environment, put the fire out.
                if (air == null || air.GetMoles(Gas.Oxygen) < 1f)
                {
                    Extinguish(uid, flammable);
                    continue;
                }

                EnsureComp<IgnitionSourceComponent>(uid);
                _ignitionSourceSystem.SetIgnited(uid);
            }
        }
    }
}
