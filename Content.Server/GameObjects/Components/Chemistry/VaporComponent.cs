using System.Linq;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    class VaporComponent : SharedVaporComponent, ICollideBehavior
    {
        public const float ReactTime = 0.125f;

        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [ViewVariables]
        private ReagentUnit _transferAmount;

        private bool _reached;
        private float _reactTimer;
        private float _timer;
        private EntityCoordinates _target;
        private bool _running;
        private Vector2 _direction;
        private float _velocity;
        private float _aliveTime;

        public override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn(out SolutionContainerComponent _);
        }

        public void Start(Vector2 dir, float velocity, EntityCoordinates target, float aliveTime)
        {
            _running = true;
            _target = target;
            _direction = dir;
            _velocity = velocity;
            _aliveTime = aliveTime;
            // Set Move
            if (Owner.TryGetComponent(out IPhysicsComponent physics))
            {
                var controller = physics.EnsureController<VaporController>();
                controller.Move(_direction, _velocity);
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _transferAmount, "transferAmount", ReagentUnit.New(0.5));
        }

        public void Update(float frameTime)
        {
            if (!Owner.TryGetComponent(out SolutionContainerComponent contents))
                return;

            if (!_running)
                return;

            _timer += frameTime;
            _reactTimer += frameTime;

            if (_reactTimer >= ReactTime && Owner.TryGetComponent(out IPhysicsComponent physics))
            {
                _reactTimer = 0;
                var mapGrid = _mapManager.GetGrid(Owner.Transform.GridID);

                var tile = mapGrid.GetTileRef(Owner.Transform.Coordinates.ToVector2i(Owner.EntityManager, _mapManager));
                foreach (var reagentQuantity in contents.ReagentList.ToArray())
                {
                    if (reagentQuantity.Quantity == ReagentUnit.Zero) continue;
                    var reagent = _prototypeManager.Index<ReagentPrototype>(reagentQuantity.ReagentId);
                    contents.TryRemoveReagent(reagentQuantity.ReagentId, reagent.ReactionTile(tile, (reagentQuantity.Quantity / _transferAmount) * 0.25f));
                }
            }

            // Check if we've reached our target.
            if(!_reached && _target.TryDistance(Owner.EntityManager, Owner.Transform.Coordinates, out var distance) && distance <= 0.5f)
            {
                _reached = true;

                if (Owner.TryGetComponent(out IPhysicsComponent coll))
                {
                    var controller = coll.EnsureController<VaporController>();
                    controller.Stop();
                }
            }

            if (contents.CurrentVolume == 0 || _timer > _aliveTime)
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

            if (!Owner.TryGetComponent(out SolutionContainerComponent contents))
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
            if (!Owner.TryGetComponent(out SolutionContainerComponent contents))
                return;

            contents.Solution.DoEntityReaction(collidedWith, ReactionMethod.Touch);

            // Check for collision with a impassable object (e.g. wall) and stop
            if (collidedWith.TryGetComponent(out IPhysicsComponent physics))
            {
                if ((physics.CollisionLayer & (int) CollisionGroup.Impassable) != 0 && physics.Hard)
                {
                    if (Owner.TryGetComponent(out IPhysicsComponent coll))
                    {
                        var controller = coll.EnsureController<VaporController>();
                        controller.Stop();
                    }

                    Owner.Delete();
                }
            }
        }
    }
}
