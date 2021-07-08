using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Microsoft.Extensions.Configuration;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Server.Shuttles
{
    internal sealed class ShuttleSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private const float TileMassMultiplier = 2f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ShuttleComponent, ComponentStartup>(HandleShuttleInit);
            SubscribeLocalEvent<ShuttleComponent, ComponentShutdown>(HandleShuttleShutdown);

            SubscribeLocalEvent<GridInitializeEvent>(HandleGridInit);

            _mapManager.TileChanged += HandleTileChange;

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CCVars.DefaultGridShuttle, _ =>
            {
                var ticker = Get<GameTicker>();
                if (!EntityManager.TryGetEntity(_mapManager.GetGrid(ticker.DefaultGridId).GridEntityId,
                    out var gridEnt) ||
                    !gridEnt.TryGetComponent(out ShuttleComponent? shuttleComponent)) return;

                Toggle(shuttleComponent);
            });
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _mapManager.TileChanged -= HandleTileChange;
        }

        private void HandleGridInit(GridInitializeEvent ev)
        {
            EntityManager.GetEntity(ev.EntityUid).EnsureComponent<ShuttleComponent>();
        }

        private void HandleTileChange(object? sender, TileChangedEventArgs e)
        {
            var oldSpace = e.OldTile.IsEmpty;
            var newSpace = e.NewTile.Tile.IsEmpty;

            if (oldSpace == newSpace) return;

            if (!EntityManager.TryGetEntity(_mapManager.GetGrid(e.NewTile.GridIndex).GridEntityId,
                out var gridEnt) ||
                !gridEnt.HasComponent<ShuttleComponent>() ||
                !gridEnt.TryGetComponent(out PhysicsComponent? physicsComponent)) return;

            float mass;

            if (oldSpace)
            {
                mass = TileMassMultiplier;
            }
            else
            {
                mass = -TileMassMultiplier;
            }

            foreach (var fixture in physicsComponent.Fixtures)
            {
                fixture.Mass += mass;
                break;
            }
        }

        private void HandleShuttleInit(EntityUid uid, ShuttleComponent component, ComponentStartup args)
        {
            if (!component.Owner.TryGetComponent(out IMapGridComponent? mapGridComp))
            {
                Logger.Error($"Tried to initialize {nameof(ShuttleComponent)} on {component.Owner} which doesn't have a mapgrid?");
                return;
            }

            if (!component.Owner.TryGetComponent(out PhysicsComponent? physicsComponent))
            {
                return;
            }

            // TODO: Look there's fixtures on chunks now but ideally empty space wouldn't be counted so I still have this garbage.
            var mass = 0f;

            foreach (var _ in mapGridComp.Grid.GetAllTiles())
            {
                mass += TileMassMultiplier;
            }

            var fixtureCount = physicsComponent.FixtureCount;

            foreach (var fixture in physicsComponent.Fixtures)
            {
                fixture.Mass = mass / fixtureCount;
            }

            var ticker = Get<GameTicker>();

            if (mapGridComp.GridIndex == ticker.DefaultGridId)
            {
                var enabled = IoCManager.Resolve<IConfigurationManager>().GetCVar(CCVars.DefaultGridShuttle);

                if (enabled != component.Enabled)
                {
                    component.Enabled = enabled;
                }
            }

            if (component.Enabled)
            {
                Enable(physicsComponent);
            }

            // TODO: Something better than this
            component.SpeedMultipler = mass / 40f;
        }

        public void Toggle(ShuttleComponent component)
        {
            if (!component.Owner.TryGetComponent(out PhysicsComponent? physicsComponent)) return;

            component.Enabled = !component.Enabled;

            if (component.Enabled)
            {
                Enable(physicsComponent);
            }
            else
            {
                Disable(physicsComponent);
            }
        }

        private void Enable(PhysicsComponent component)
        {
            component.BodyType = BodyType.Dynamic;
            component.BodyStatus = BodyStatus.InAir;
            component.FixedRotation = false;
        }

        private void Disable(PhysicsComponent component)
        {
            component.BodyType = BodyType.Static;
            component.BodyStatus = BodyStatus.OnGround;
            component.FixedRotation = true;
        }

        private void HandleShuttleShutdown(EntityUid uid, ShuttleComponent component, ComponentShutdown args)
        {
            if (!component.Owner.TryGetComponent(out PhysicsComponent? physicsComponent))
            {
                return;
            }

            Disable(physicsComponent);

            foreach (var fixture in physicsComponent.Fixtures)
            {
                fixture.Mass = 0f;
                break;
            }
        }
    }
}
