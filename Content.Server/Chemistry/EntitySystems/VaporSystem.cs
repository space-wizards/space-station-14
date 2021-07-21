using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    internal sealed class VaporSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _protoManager = default!;

        private const float ReactTime = 0.125f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VaporComponent, StartCollideEvent>(HandleCollide);
        }

        private void HandleCollide(EntityUid uid, VaporComponent component, StartCollideEvent args)
        {
            if (!ComponentManager.TryGetComponent(uid, out SolutionContainerComponent? contents)) return;

            contents.Solution.DoEntityReaction(args.OtherFixture.Body.Owner, ReactionMethod.Touch);

            // Check for collision with a impassable object (e.g. wall) and stop
            if ((args.OtherFixture.CollisionLayer & (int) CollisionGroup.Impassable) != 0 && args.OtherFixture.Hard)
            {
               EntityManager.QueueDeleteEntity(uid);
            }
        }

        public void Start(VaporComponent vapor, Vector2 dir, float speed, EntityCoordinates target, float aliveTime)
        {
            vapor.Active = true;
            vapor.Target = target;
            vapor.AliveTime = aliveTime;
            // Set Move
            if (vapor.Owner.TryGetComponent(out PhysicsComponent? physics))
            {
                physics.BodyStatus = BodyStatus.InAir;
                physics.ApplyLinearImpulse(dir * speed);
            }
        }

        internal bool TryAddSolution(VaporComponent vapor, Solution solution)
        {
            if (solution.TotalVolume == 0)
            {
                return false;
            }

            if (!vapor.Owner.TryGetComponent(out SolutionContainerComponent? contents))
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

        public override void Update(float frameTime)
        {
            foreach (var (vaporComp, solution) in ComponentManager.EntityQuery<VaporComponent, SolutionContainerComponent>(true))
            {
                Update(frameTime, vaporComp, solution);
            }
        }

        private void Update(float frameTime, VaporComponent vapor, SolutionContainerComponent contents)
        {
            if (!vapor.Active)
                return;

            var entity = vapor.Owner;

            vapor.Timer += frameTime;
            vapor.ReactTimer += frameTime;

            if (vapor.ReactTimer >= ReactTime && vapor.Owner.Transform.GridID.IsValid())
            {
                vapor.ReactTimer = 0;
                var mapGrid = _mapManager.GetGrid(entity.Transform.GridID);

                var tile = mapGrid.GetTileRef(entity.Transform.Coordinates.ToVector2i(EntityManager, _mapManager));
                foreach (var reagentQuantity in contents.ReagentList.ToArray())
                {
                    if (reagentQuantity.Quantity == ReagentUnit.Zero) continue;
                    var reagent = _protoManager.Index<ReagentPrototype>(reagentQuantity.ReagentId);
                    contents.TryRemoveReagent(reagentQuantity.ReagentId, reagent.ReactionTile(tile, (reagentQuantity.Quantity / vapor.TransferAmount) * 0.25f));
                }
            }

            // Check if we've reached our target.
            if(!vapor.Reached && vapor.Target.TryDistance(EntityManager, entity.Transform.Coordinates, out var distance) && distance <= 0.5f)
            {
                vapor.Reached = true;
            }

            if (contents.CurrentVolume == 0 || vapor.Timer > vapor.AliveTime)
            {
                // Delete this
                entity.QueueDelete();
            }
        }
    }
}
