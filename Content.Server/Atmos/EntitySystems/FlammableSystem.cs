using Content.Server.Administration.Logs;
using Content.Server.Atmos.Components;
using Content.Server.IgnitionSource;
using Content.Server.Stunnable;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Server.Damage.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Rejuvenate;
using Content.Shared.Temperature;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.FixedPoint;
using Robust.Server.Audio;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Content.Server.Inventory;
using Content.Shared.Inventory;
using Content.Server.Inventory;
using Content.Server.Inventory.Events;
using Content.Shared.Maps;

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
        [Dependency] private readonly FixtureSystem _fixture = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly UseDelaySystem _useDelay = default!;
        [Dependency] private readonly AudioSystem _audio = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private EntityQuery<InventoryComponent> _inventoryQuery;
        private EntityQuery<PhysicsComponent> _physicsQuery;

        private const float UpdateTime = 1f;
        private float _timer;

        private readonly Dictionary<Entity<FlammableComponent>, float> _fireEvents = new();

        public override void Initialize()
        {
            UpdatesAfter.Add(typeof(AtmosphereSystem));

            _inventoryQuery = GetEntityQuery<InventoryComponent>();
            _physicsQuery = GetEntityQuery<PhysicsComponent>();

            SubscribeLocalEvent<FlammableComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<FlammableComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<FlammableComponent, StartCollideEvent>(OnCollide);
            SubscribeLocalEvent<FlammableComponent, IsHotEvent>(OnIsHot);
            SubscribeLocalEvent<FlammableComponent, TileFireEvent>(OnTileFire);
            SubscribeLocalEvent<FlammableComponent, RejuvenateEvent>(OnRejuvenate);
            SubscribeLocalEvent<EquippedHandEvent>(OnEquippedHandEvent);
            SubscribeLocalEvent<IgniteOnCollideComponent, StartCollideEvent>(IgniteOnCollide);
            SubscribeLocalEvent<IgniteOnCollideComponent, LandEvent>(OnIgniteLand);
            SubscribeLocalEvent<IgniteOnMeleeHitComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<ExtinguishOnInteractComponent, ActivateInWorldEvent>(OnExtinguishActivateInWorld);
            SubscribeLocalEvent<IgniteOnHeatDamageComponent, DamageChangedEvent>(OnDamageChanged);
        }

        private void OnEquippedHandEvent(EquippedHandEvent args)
        {
            Log.Error($"Object picked up");
            // Get the item that was equipped
            var equippedItem = args.Equipped;

            if (!HasComp<JellidComponent>(playerUid))
            {
                Log.Error($"Player is not a Jellid");
                return;
            }

            // Check if the item is flammable
            if (TryComp<FlammableComponent>(equippedItem, out var flammable))
            {
                // Ignite the item if it is flammable
                Ignite(equippedItem, equippedItem, flammable, args.User); //Make sure this aligns with Ignite logic properly
                Log.Error($"Object ignited");
            }
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

            //Only ignite when the colliding fixture is projectile or ignition.
            if (args.OurFixtureId != component.FixtureId && args.OurFixtureId != SharedProjectileSystem.ProjectileFixture)
            {
                return;
            }

            flammable.FireStacks += component.FireStacks;
            Ignite(otherEnt, uid, flammable);
            component.Count--;

            if (component.Count == 0)
                RemCompDeferred<IgniteOnCollideComponent>(uid);
        }

        private void OnMapInit(EntityUid uid, FlammableComponent component, MapInitEvent args)
        {
            // Sets up a fixture for flammable collisions.
            if (!TryComp<PhysicsComponent>(uid, out var body))
                return;

            _fixture.TryCreateFixture(uid, component.FlammableCollisionShape, component.FlammableFixtureID, hard: false,
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
            if (args.Handled || !args.Complex)
                return;

            if (!TryComp(uid, out FlammableComponent? flammable))
                return;

            if (!flammable.OnFire)
                return;

            args.Handled = true;

            if (!TryComp(uid, out UseDelayComponent? useDelay) || !_useDelay.TryResetDelay((uid, useDelay), true))
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
            if (args.OtherFixtureId != flammable.FlammableFixtureID && args.OurFixtureId != flammable.FlammableFixtureID)
                return;

            if (!flammable.FireSpread)
                return;

            if (!TryComp(otherUid, out FlammableComponent? otherFlammable) || !otherFlammable.FireSpread)
                return;

            if (!flammable.OnFire && !otherFlammable.OnFire)
                return; // Neither are on fire

            // Both are on fire -> equalize fire stacks.
            var mass1 = 1f;
            var mass2 = 1f;
            if (_physicsQuery.TryComp(uid, out var physics) && _physicsQuery.TryComp(otherUid, out var otherPhys))
            {
                mass1 = physics.Mass;
                mass2 = otherPhys.Mass;
            }

            var total = mass1 + mass2;
            var avg = (flammable.FireStacks + otherFlammable.FireStacks) / total;

            var (src, dest) = flammable.FireStacks * mass1 > otherFlammable.FireStacks * mass2
                ? (-1f, 1f)
                : (1f, -1f);

            AdjustFireStacks(uid, src * avg * mass2, flammable, ignite: true);
            AdjustFireStacks(otherUid, dest * avg * mass1, otherFlammable, ignite: true);
        }

        private void OnTileFire(EntityUid uid, FlammableComponent flammable, TileFireEvent args)
        {
            if (flammable.OnFire)
                return;

            Ignite(uid, args.Source, flammable);
        }

        private void OnRejuvenate(EntityUid uid, FlammableComponent flammable, RejuvenateEvent args)
        {
            if (flammable.OnFire)
                return;

            flammable.FireStacks = 0;
            UpdateAppearance(uid, flammable);
        }

        private void Ignite(EntityUid uid, EntityUid ignitionSource, FlammableComponent? flammable = null, EntityUid? ignitionSourceUser = null)
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
                    _adminLogger.Add(LogType.Flammable, $"{ToPrettyString(uid):target} set on fire by {ToPrettyString(ignitionSourceUser.Value):actor} with {ToPrettyString(ignitionSource):tool}.");
                else
                    _adminLogger.Add(LogType.Flammable, $"{ToPrettyString(uid):target} set on fire by {ToPrettyString(ignitionSource):actor}.");

                flammable.OnFire = true;
            }

            UpdateAppearance(uid, flammable);
        }

        private void AdjustFireStacks(EntityUid uid, float stackDelta, FlammableComponent flammable, bool ignite = false)
        {
            flammable.FireStacks = Math.Clamp(flammable.FireStacks + stackDelta, 0, flammable.MaxFireStacks);
            if (ignite)
                Ignite(uid, uid, flammable);
            UpdateAppearance(uid, flammable);
        }

        private void UpdateAppearance(EntityUid uid, FlammableComponent flammable)
        {
            _appearance.SetAppearance(uid, flammable.OnFire ? "fire" : "no_fire");
        }
    }
}
