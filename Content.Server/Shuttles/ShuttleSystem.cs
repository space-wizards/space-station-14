using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Content.Server.Shuttles
{
    internal sealed class ShuttleSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private const float TileMassMultiplier = 2f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ShuttleComponent, ComponentInit>(HandleShuttleInit);
            SubscribeLocalEvent<ShuttleComponent, ComponentShutdown>(HandleShuttleShutdown);

            SubscribeLocalEvent<GridInitializeEvent>(HandleGridInit);

            _mapManager.TileChanged += HandleTileChange;
        }

        private void HandleGridInit(GridInitializeEvent ev)
        {
            // TODO: TEMPORARY DEBUGGING
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

        private void HandleShuttleInit(EntityUid uid, ShuttleComponent component, ComponentInit args)
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

            physicsComponent.BodyType = BodyType.Dynamic;
            physicsComponent.BodyStatus = BodyStatus.InAir;
            physicsComponent.FixedRotation = false;

            var mass = 0f;

            // TODO: Ideally we'd have fixtures on each chunk and could just use those as they would be actively maintained
            // instead of this garbage.
            foreach (var _ in mapGridComp.Grid.GetAllTiles())
            {
                mass += TileMassMultiplier;
            }

            // TODO: fixture per chunk or whatever. Need to mess around with MapChunk's collision box thing.
            // Iterate each fixture and give it mass at the same time too
            foreach (var fixture in physicsComponent.Fixtures)
            {
                fixture.Mass = mass;
                break;
            }
        }

        private void HandleShuttleShutdown(EntityUid uid, ShuttleComponent component, ComponentShutdown args)
        {
            if (!component.Owner.TryGetComponent(out PhysicsComponent? physicsComponent))
            {
                return;
            }

            physicsComponent.BodyType = BodyType.Static;
            physicsComponent.BodyStatus = BodyStatus.OnGround;
            physicsComponent.FixedRotation = true;

            foreach (var fixture in physicsComponent.Fixtures)
            {
                fixture.Mass = 0f;
                break;
            }
        }
    }
}
