using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Physics;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using System.Numerics;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    internal sealed class VaporSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly SharedMapSystem _map = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly ThrowingSystem _throwing = default!;
        [Dependency] private readonly ReactiveSystem _reactive = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

        private const float ReactTime = 0.125f;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<VaporComponent, StartCollideEvent>(HandleCollide);
        }

        private void HandleCollide(Entity<VaporComponent> entity, ref StartCollideEvent args)
        {
            if (!EntityManager.TryGetComponent(entity.Owner, out SolutionContainerManagerComponent? contents)) return;

            foreach (var (_, soln) in _solutionContainerSystem.EnumerateSolutions((entity.Owner, contents)))
            {
                var solution = soln.Comp.Solution;
                _reactive.DoEntityReaction(args.OtherEntity, solution, ReactionMethod.Touch);
            }

            // Check for collision with a impassable object (e.g. wall) and stop
            if ((args.OtherFixture.CollisionLayer & (int) CollisionGroup.Impassable) != 0 && args.OtherFixture.Hard)
            {
                EntityManager.QueueDeleteEntity(entity);
            }
        }

        public void Start(Entity<VaporComponent> vapor, TransformComponent vaporXform, Vector2 dir, float speed, MapCoordinates target, float aliveTime, EntityUid? user = null)
        {
            vapor.Comp.Active = true;
            var despawn = EnsureComp<TimedDespawnComponent>(vapor);
            despawn.Lifetime = aliveTime;

            // Set Move
            if (EntityManager.TryGetComponent(vapor, out PhysicsComponent? physics))
            {
                _physics.SetLinearDamping(vapor, physics, 0f);
                _physics.SetAngularDamping(vapor, physics, 0f);

                _throwing.TryThrow(vapor, dir, speed, user: user);

                var distance = (target.Position - _transformSystem.GetWorldPosition(vaporXform)).Length();
                var time = (distance / physics.LinearVelocity.Length());
                despawn.Lifetime = MathF.Min(aliveTime, time);
            }
        }

        internal bool TryAddSolution(Entity<VaporComponent> vapor, Solution solution)
        {
            if (solution.Volume == 0)
            {
                return false;
            }

            if (!_solutionContainerSystem.TryGetSolution(vapor.Owner, VaporComponent.SolutionName, out var vaporSolution))
            {
                return false;
            }

            return _solutionContainerSystem.TryAddSolution(vaporSolution.Value, solution);
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<VaporComponent, SolutionContainerManagerComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var vaporComp, out var container, out var xform))
            {
                foreach (var (_, soln) in _solutionContainerSystem.EnumerateSolutions((uid, container)))
                {
                    Update(frameTime, (uid, vaporComp), soln, xform);
                }
            }
        }

        private void Update(float frameTime, Entity<VaporComponent> ent, Entity<SolutionComponent> soln, TransformComponent xform)
        {
            var (entity, vapor) = ent;
            if (!vapor.Active)
                return;

            vapor.ReactTimer += frameTime;

            var contents = soln.Comp.Solution;
            if (vapor.ReactTimer >= ReactTime && TryComp(xform.GridUid, out MapGridComponent? gridComp))
            {
                vapor.ReactTimer = 0;

                var tile = _map.GetTileRef(xform.GridUid.Value, gridComp, xform.Coordinates);
                foreach (var reagentQuantity in contents.Contents.ToArray())
                {
                    if (reagentQuantity.Quantity == FixedPoint2.Zero) continue;
                    var reagent = _protoManager.Index<ReagentPrototype>(reagentQuantity.Reagent.Prototype);

                    var reaction =
                        reagent.ReactionTile(tile, (reagentQuantity.Quantity / vapor.TransferAmount) * 0.25f, EntityManager, reagentQuantity.Reagent.Data);

                    if (reaction > reagentQuantity.Quantity)
                    {
                        Log.Error($"Tried to tile react more than we have for reagent {reagentQuantity}. Found {reaction} and we only have {reagentQuantity.Quantity}");
                        reaction = reagentQuantity.Quantity;
                    }

                    _solutionContainerSystem.RemoveReagent(soln, reagentQuantity.Reagent, reaction);
                }
            }

            if (contents.Volume == 0)
            {
                // Delete this
                EntityManager.QueueDeleteEntity(entity);
            }
        }
    }
}
