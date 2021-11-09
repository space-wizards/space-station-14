using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Physics;
using Content.Shared.Vapor;
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
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

        private const float ReactTime = 0.125f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VaporComponent, StartCollideEvent>(HandleCollide);
        }

        private void HandleCollide(EntityUid uid, VaporComponent component, StartCollideEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out SolutionContainerManagerComponent? contents)) return;

            foreach (var (_, value) in contents.Solutions)
            {
                value.DoEntityReaction(args.OtherFixture.Body.Owner.Uid, ReactionMethod.Touch);
            }

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

            if (!_solutionContainerSystem.TryGetSolution(vapor.Owner.Uid, SharedVaporComponent.SolutionName,
                out var vaporSolution))
            {
                return false;
            }

            return _solutionContainerSystem.TryAddSolution(vapor.Owner.Uid, vaporSolution, solution);
        }

        public override void Update(float frameTime)
        {
            foreach (var (vaporComp, solution) in EntityManager
                .EntityQuery<VaporComponent, SolutionContainerManagerComponent>())
            {
                foreach (var (_, value) in solution.Solutions)
                {
                    Update(frameTime, vaporComp, value);
                }
            }
        }

        private void Update(float frameTime, VaporComponent vapor, Solution contents)
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
                foreach (var reagentQuantity in contents.Contents.ToArray())
                {
                    if (reagentQuantity.Quantity == FixedPoint2.Zero) continue;
                    var reagent = _protoManager.Index<ReagentPrototype>(reagentQuantity.ReagentId);
                    _solutionContainerSystem.TryRemoveReagent(vapor.Owner.Uid, contents, reagentQuantity.ReagentId,
                        reagent.ReactionTile(tile, (reagentQuantity.Quantity / vapor.TransferAmount) * 0.25f));
                }
            }

            // Check if we've reached our target.
            if (!vapor.Reached &&
                vapor.Target.TryDistance(EntityManager, entity.Transform.Coordinates, out var distance) &&
                distance <= 0.5f)
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
