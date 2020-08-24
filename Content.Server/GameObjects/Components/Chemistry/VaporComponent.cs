using System.Linq;
using Content.Server.GameObjects.Components.Fluids;
using Content.Shared.Chemistry;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    class VaporComponent : Component, ICollideBehavior
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        public override string Name => "Vapor";

        [ViewVariables]
        private ReagentUnit _transferAmount;

        private bool _running;
        private Vector2 _direction;
        private float _velocity;

        public override void Initialize()
        {
            base.Initialize();

            if (!Owner.EnsureComponent(out SolutionComponent _))
            {
                Logger.Warning(
                    $"Entity {Owner.Name} at {Owner.Transform.MapPosition} didn't have a {nameof(SolutionComponent)}");
            }
        }

        public void Start(Vector2 dir, float velocity)
        {
            _running = true;
            _direction = dir;
            _velocity = velocity;
            // Set Move
            if (Owner.TryGetComponent(out ICollidableComponent collidable))
            {
                var controller = collidable.EnsureController<VaporController>();
                controller.Move(_direction, _velocity);
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _transferAmount, "transferAmount", ReagentUnit.New(0.5));
        }

        public void Update()
        {
            if (!Owner.TryGetComponent(out SolutionComponent contents))
                return;

            if (!_running)
                return;

            // Get all intersecting tiles with the vapor and spray the divided solution on there
            if (Owner.TryGetComponent(out ICollidableComponent collidable))
            {
                var worldBounds = collidable.WorldAABB;
                var mapGrid = _mapManager.GetGrid(Owner.Transform.GridID);

                var tiles = mapGrid.GetTilesIntersecting(worldBounds);
                var amount = _transferAmount / ReagentUnit.New(tiles.Count());
                foreach (var tile in tiles)
                {
                    var pos = tile.GridIndices.ToGridCoordinates(_mapManager, tile.GridIndex);
                    SpillHelper.SpillAt(pos, contents.SplitSolution(amount), "PuddleSmear", false); //make non PuddleSmear?
                }
            }

            if (contents.CurrentVolume == 0)
            {
                // Delete this
                Owner.Delete();
            }
        }

        internal bool TryAddSolution(Solution solution)
        {
            if (solution.TotalVolume == 0)
            {
                return false;
            }

            if (!Owner.TryGetComponent(out SolutionComponent contents))
            {
                return false;
            }

            var result = contents.TryAddSolution(solution);

            if (!result)
            {
                return false;
            }

            return true;
        }

        void ICollideBehavior.CollideWith(IEntity collidedWith)
        {
            // Check for collision with a impassable object (e.g. wall) and stop
            if (collidedWith.TryGetComponent(out ICollidableComponent collidable))
            {
                if ((collidable.CollisionLayer & (int) CollisionGroup.Impassable) != 0 && collidable.Hard)
                {
                    if (Owner.TryGetComponent(out ICollidableComponent coll))
                    {
                        var controller = coll.EnsureController<VaporController>();
                        controller.Stop();
                    }
                }
            }
        }
    }
}
