using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.Audio;
using Content.Server.Construction;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Shuttles.Components;
using Content.Shared.Temperature;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems
{
    public sealed class ThrusterSystem : EntitySystem
    {
        [Robust.Shared.IoC.Dependency] private readonly IMapManager _mapManager = default!;
        [Robust.Shared.IoC.Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
        [Robust.Shared.IoC.Dependency] private readonly AmbientSoundSystem _ambient = default!;
        [Robust.Shared.IoC.Dependency] private readonly FixtureSystem _fixtureSystem = default!;
        [Robust.Shared.IoC.Dependency] private readonly DamageableSystem _damageable = default!;

        // Essentially whenever thruster enables we update the shuttle's available impulses which are used for movement.
        // This is done for each direction available.

        public const string BurnFixture = "thruster-burn";

        private readonly HashSet<ThrusterComponent> _activeThrusters = new();

        // Used for accumulating burn if someone touches a firing thruster.

        private float _accumulator;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ThrusterComponent, ActivateInWorldEvent>(OnActivateThruster);
            SubscribeLocalEvent<ThrusterComponent, ComponentInit>(OnThrusterInit);
            SubscribeLocalEvent<ThrusterComponent, ComponentShutdown>(OnThrusterShutdown);
            SubscribeLocalEvent<ThrusterComponent, PowerChangedEvent>(OnPowerChange);
            SubscribeLocalEvent<ThrusterComponent, AnchorStateChangedEvent>(OnAnchorChange);
            SubscribeLocalEvent<ThrusterComponent, ReAnchorEvent>(OnThrusterReAnchor);
            SubscribeLocalEvent<ThrusterComponent, MoveEvent>(OnRotate);
            SubscribeLocalEvent<ThrusterComponent, IsHotEvent>(OnIsHotEvent);
            SubscribeLocalEvent<ThrusterComponent, StartCollideEvent>(OnStartCollide);
            SubscribeLocalEvent<ThrusterComponent, EndCollideEvent>(OnEndCollide);

            SubscribeLocalEvent<ThrusterComponent, ExaminedEvent>(OnThrusterExamine);

            SubscribeLocalEvent<ThrusterComponent, RefreshPartsEvent>(OnRefreshParts);
            SubscribeLocalEvent<ThrusterComponent, UpgradeExamineEvent>(OnUpgradeExamine);

            SubscribeLocalEvent<ShuttleComponent, TileChangedEvent>(OnShuttleTileChange);
        }

        private void OnThrusterExamine(EntityUid uid, ThrusterComponent component, ExaminedEvent args)
        {
            // Powered is already handled by other power components
            var enabled = Loc.GetString(component.Enabled ? "thruster-comp-enabled" : "thruster-comp-disabled");

            args.PushMarkup(enabled);

            if (component.Type == ThrusterType.Linear &&
                EntityManager.TryGetComponent(uid, out TransformComponent? xform) &&
                xform.Anchored)
            {
                var nozzleDir = Loc.GetString("thruster-comp-nozzle-direction",
                    ("direction", xform.LocalRotation.Opposite().ToWorldVec().GetDir().ToString().ToLowerInvariant()));

                args.PushMarkup(nozzleDir);

                var exposed = NozzleExposed(xform);

                var nozzleText = Loc.GetString(exposed ? "thruster-comp-nozzle-exposed" : "thruster-comp-nozzle-not-exposed");

                args.PushMarkup(nozzleText);
            }
        }

        private void OnIsHotEvent(EntityUid uid, ThrusterComponent component, IsHotEvent args)
        {
            args.IsHot = component.Type != ThrusterType.Angular && component.IsOn;
        }

        private void OnShuttleTileChange(EntityUid uid, ShuttleComponent component, ref TileChangedEvent args)
        {
            // If the old tile was space but the new one isn't then disable all adjacent thrusters
            if (args.NewTile.IsSpace(_tileDefManager) || !args.OldTile.IsSpace(_tileDefManager)) return;

            var tilePos = args.NewTile.GridIndices;
            var grid = _mapManager.GetGrid(uid);
            var xformQuery = GetEntityQuery<TransformComponent>();
            var thrusterQuery = GetEntityQuery<ThrusterComponent>();

            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    if (x != 0 && y != 0) continue;

                    var checkPos = tilePos + new Vector2i(x, y);
                    var enumerator = grid.GetAnchoredEntitiesEnumerator(checkPos);

                    while (enumerator.MoveNext(out var ent))
                    {
                        if (!thrusterQuery.TryGetComponent(ent.Value, out var thruster) || !thruster.RequireSpace) continue;

                        // Work out if the thruster is facing this direction
                        var xform = xformQuery.GetComponent(ent.Value);
                        var direction = xform.LocalRotation.ToWorldVec();

                        if (new Vector2i((int) direction.X, (int) direction.Y) != new Vector2i(x, y)) continue;

                        DisableThruster(ent.Value, thruster, xform.GridUid);
                    }
                }
            }
        }

        private void OnActivateThruster(EntityUid uid, ThrusterComponent component, ActivateInWorldEvent args)
        {
            component.Enabled ^= true;
        }

        /// <summary>
        /// If the thruster rotates change the direction where the linear thrust is applied
        /// </summary>
        private void OnRotate(EntityUid uid, ThrusterComponent component, ref MoveEvent args)
        {
            // TODO: Disable visualizer for old direction

            if (!component.Enabled ||
                component.Type != ThrusterType.Linear ||
                !EntityManager.TryGetComponent(uid, out TransformComponent? xform) ||
                !_mapManager.TryGetGrid(xform.GridUid, out var grid) ||
                !EntityManager.TryGetComponent(grid.Owner, out ShuttleComponent? shuttleComponent))
            {
                return;
            }

            var canEnable = CanEnable(uid, component);

            // If it's not on then don't enable it inadvertantly (given we don't have an old rotation)
            if (!canEnable && !component.IsOn) return;

            // Enable it if it was turned off but new tile is valid
            if (!component.IsOn && canEnable)
            {
                EnableThruster(uid, component);
                return;
            }

            // Disable if new tile invalid
            if (component.IsOn && !canEnable)
            {
                DisableThruster(uid, component, xform, args.OldRotation);
                return;
            }

            var oldDirection = (int) args.OldRotation.GetCardinalDir() / 2;
            var direction = (int) args.NewRotation.GetCardinalDir() / 2;

            shuttleComponent.LinearThrust[oldDirection] -= component.Thrust;
            DebugTools.Assert(shuttleComponent.LinearThrusters[oldDirection].Contains(component));
            shuttleComponent.LinearThrusters[oldDirection].Remove(component);

            shuttleComponent.LinearThrust[direction] += component.Thrust;
            DebugTools.Assert(!shuttleComponent.LinearThrusters[direction].Contains(component));
            shuttleComponent.LinearThrusters[direction].Add(component);
        }

        private void OnAnchorChange(EntityUid uid, ThrusterComponent component, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored && CanEnable(uid, component))
            {
                EnableThruster(uid, component);
            }
            else
            {
                DisableThruster(uid, component);
            }
        }

        private void OnThrusterReAnchor(EntityUid uid, ThrusterComponent component, ref ReAnchorEvent args)
        {
            DisableThruster(uid, component, args.OldGrid);

            if (CanEnable(uid, component))
                EnableThruster(uid, component);
        }

        private void OnThrusterInit(EntityUid uid, ThrusterComponent component, ComponentInit args)
        {
            _ambient.SetAmbience(uid, false);

            if (!component.Enabled)
            {
                return;
            }

            if (CanEnable(uid, component))
            {
                EnableThruster(uid, component);
            }
        }

        private void OnThrusterShutdown(EntityUid uid, ThrusterComponent component, ComponentShutdown args)
        {
            DisableThruster(uid, component);
        }

        private void OnPowerChange(EntityUid uid, ThrusterComponent component, ref PowerChangedEvent args)
        {
            if (args.Powered && CanEnable(uid, component))
            {
                EnableThruster(uid, component);
            }
            else
            {
                DisableThruster(uid, component);
            }
        }

        /// <summary>
        /// Tries to enable the thruster and turn it on. If it's already enabled it does nothing.
        /// </summary>
        public void EnableThruster(EntityUid uid, ThrusterComponent component, TransformComponent? xform = null)
        {
            if (component.IsOn ||
                !Resolve(uid, ref xform) ||
                !_mapManager.TryGetGrid(xform.GridUid, out var grid)) return;

            component.IsOn = true;

            if (!EntityManager.TryGetComponent(grid.Owner, out ShuttleComponent? shuttleComponent)) return;

            // Logger.DebugS("thruster", $"Enabled thruster {uid}");

            switch (component.Type)
            {
                case ThrusterType.Linear:
                    var direction = (int) xform.LocalRotation.GetCardinalDir() / 2;

                    shuttleComponent.LinearThrust[direction] += component.Thrust;
                    DebugTools.Assert(!shuttleComponent.LinearThrusters[direction].Contains(component));
                    shuttleComponent.LinearThrusters[direction].Add(component);

                    // Don't just add / remove the fixture whenever the thruster fires because perf
                    if (EntityManager.TryGetComponent(uid, out PhysicsComponent? physicsComponent) &&
                        component.BurnPoly.Count > 0)
                    {
                        var shape = new PolygonShape();

                        shape.SetVertices(component.BurnPoly);

                        var fixture = new Fixture(physicsComponent, shape)
                        {
                            ID = BurnFixture,
                            Hard = false,
                            CollisionLayer = (int) CollisionGroup.FullTileMask
                        };

                        _fixtureSystem.TryCreateFixture(physicsComponent, fixture);
                    }

                    break;
                case ThrusterType.Angular:
                    shuttleComponent.AngularThrust += component.Thrust;
                    DebugTools.Assert(!shuttleComponent.AngularThrusters.Contains(component));
                    shuttleComponent.AngularThrusters.Add(component);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearanceComponent))
            {
                appearanceComponent.SetData(ThrusterVisualState.State, true);
            }

            if (EntityManager.TryGetComponent(uid, out PointLightComponent? pointLightComponent))
            {
                pointLightComponent.Enabled = true;
            }

            _ambient.SetAmbience(uid, true);
        }

        public void DisableThruster(EntityUid uid, ThrusterComponent component, TransformComponent? xform = null, Angle? angle = null)
        {
            if (!Resolve(uid, ref xform)) return;
            DisableThruster(uid, component, xform.GridUid, xform);
        }

        /// <summary>
        /// Tries to disable the thruster.
        /// </summary>
        public void DisableThruster(EntityUid uid, ThrusterComponent component, EntityUid? gridId, TransformComponent? xform = null, Angle? angle = null)
        {
            if (!component.IsOn ||
                !Resolve(uid, ref xform) ||
                !_mapManager.TryGetGrid(gridId, out var grid)) return;

            component.IsOn = false;

            if (!EntityManager.TryGetComponent(grid.Owner, out ShuttleComponent? shuttleComponent)) return;

            // Logger.DebugS("thruster", $"Disabled thruster {uid}");

            switch (component.Type)
            {
                case ThrusterType.Linear:
                    angle ??= xform.LocalRotation;
                    var direction = (int) angle.Value.GetCardinalDir() / 2;

                    shuttleComponent.LinearThrust[direction] -= component.Thrust;
                    DebugTools.Assert(shuttleComponent.LinearThrusters[direction].Contains(component));
                    shuttleComponent.LinearThrusters[direction].Remove(component);
                    break;
                case ThrusterType.Angular:
                    shuttleComponent.AngularThrust -= component.Thrust;
                    DebugTools.Assert(shuttleComponent.AngularThrusters.Contains(component));
                    shuttleComponent.AngularThrusters.Remove(component);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearanceComponent))
            {
                appearanceComponent.SetData(ThrusterVisualState.State, false);
            }

            if (EntityManager.TryGetComponent(uid, out PointLightComponent? pointLightComponent))
            {
                pointLightComponent.Enabled = false;
            }

            _ambient.SetAmbience(uid, false);

            if (EntityManager.TryGetComponent(uid, out PhysicsComponent? physicsComponent))
            {
                _fixtureSystem.DestroyFixture(physicsComponent, BurnFixture);
            }

            _activeThrusters.Remove(component);
            component.Colliding.Clear();
        }

        public bool CanEnable(EntityUid uid, ThrusterComponent component)
        {
            if (!component.Enabled) return false;
            if (component.LifeStage > ComponentLifeStage.Running) return false;

            var xform = Transform(uid);

            if (!xform.Anchored ||!this.IsPowered(uid, EntityManager))
            {
                return false;
            }

            if (!component.RequireSpace)
                return true;

            return NozzleExposed(xform);
        }

        private bool NozzleExposed(TransformComponent xform)
        {
            if (xform.GridUid == null)
                return true;

            var (x, y) = xform.LocalPosition + xform.LocalRotation.Opposite().ToWorldVec();
            var tile = _mapManager.GetGrid(xform.GridUid.Value).GetTileRef(new Vector2i((int) Math.Floor(x), (int) Math.Floor(y)));

            return tile.Tile.IsSpace();
        }

        #region Burning

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _accumulator += frameTime;

            if (_accumulator < 1) return;

            _accumulator -= 1;

            foreach (var comp in _activeThrusters.ToArray())
            {
                MetaDataComponent? metaData = null;

                if (!comp.Firing || comp.Damage == null || Paused(comp.Owner, metaData) || Deleted(comp.Owner, metaData)) continue;

                DebugTools.Assert(comp.Colliding.Count > 0);

                foreach (var uid in comp.Colliding.ToArray())
                {
                    _damageable.TryChangeDamage(uid, comp.Damage);
                }
            }
        }

        private void OnStartCollide(EntityUid uid, ThrusterComponent component, ref StartCollideEvent args)
        {
            if (args.OurFixture.ID != BurnFixture) return;

            _activeThrusters.Add(component);
            component.Colliding.Add((args.OtherFixture.Body).Owner);
        }

        private void OnEndCollide(EntityUid uid, ThrusterComponent component, ref EndCollideEvent args)
        {
            if (args.OurFixture.ID != BurnFixture) return;

            component.Colliding.Remove((args.OtherFixture.Body).Owner);

            if (component.Colliding.Count == 0)
            {
                _activeThrusters.Remove(component);
            }
        }

        /// <summary>
        /// Considers a thrust direction as being active.
        /// </summary>
        public void EnableLinearThrustDirection(ShuttleComponent component, DirectionFlag direction)
        {
            if ((component.ThrustDirections & direction) != 0x0) return;

            component.ThrustDirections |= direction;

            var index = GetFlagIndex(direction);

            foreach (var comp in component.LinearThrusters[index])
            {
                if (!EntityManager.TryGetComponent((comp).Owner, out AppearanceComponent? appearanceComponent))
                    continue;

                comp.Firing = true;
                appearanceComponent.SetData(ThrusterVisualState.Thrusting, true);
            }
        }

        /// <summary>
        /// Disables a thrust direction.
        /// </summary>
        public void DisableLinearThrustDirection(ShuttleComponent component, DirectionFlag direction)
        {
            if ((component.ThrustDirections & direction) == 0x0) return;

            component.ThrustDirections &= ~direction;

            var index = GetFlagIndex(direction);

            foreach (var comp in component.LinearThrusters[index])
            {
                if (!EntityManager.TryGetComponent((comp).Owner, out AppearanceComponent? appearanceComponent))
                    continue;

                comp.Firing = false;
                appearanceComponent.SetData(ThrusterVisualState.Thrusting, false);
            }
        }

        public void DisableLinearThrusters(ShuttleComponent component)
        {
            foreach (DirectionFlag dir in Enum.GetValues(typeof(DirectionFlag)))
            {
                DisableLinearThrustDirection(component, dir);
            }

            DebugTools.Assert(component.ThrustDirections == DirectionFlag.None);
        }

        public void SetAngularThrust(ShuttleComponent component, bool on)
        {
            if (on)
            {
                foreach (var comp in component.AngularThrusters)
                {
                    if (!EntityManager.TryGetComponent((comp).Owner, out AppearanceComponent? appearanceComponent))
                        continue;

                    comp.Firing = true;
                    appearanceComponent.SetData(ThrusterVisualState.Thrusting, true);
                }
            }
            else
            {
                foreach (var comp in component.AngularThrusters)
                {
                    if (!EntityManager.TryGetComponent((comp).Owner, out AppearanceComponent? appearanceComponent))
                        continue;

                    comp.Firing = false;
                    appearanceComponent.SetData(ThrusterVisualState.Thrusting, false);
                }
            }
        }

        private void OnRefreshParts(EntityUid uid, ThrusterComponent component, RefreshPartsEvent args)
        {
            var thrustRating = args.PartRatings[component.MachinePartThrust];

            component.Thrust = component.BaseThrust * MathF.Pow(component.PartRatingThrustMultiplier, thrustRating - 1);
        }

        private void OnUpgradeExamine(EntityUid uid, ThrusterComponent component, UpgradeExamineEvent args)
        {
            args.AddPercentageUpgrade("thruster-comp-upgrade-thrust", component.Thrust / component.BaseThrust);
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetFlagIndex(DirectionFlag flag)
        {
            return (int) Math.Log2((int) flag);
        }
    }
}
