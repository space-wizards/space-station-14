using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.AI.Components;
using Content.Server.AI.Pathfinding;
using Content.Server.AI.Pathfinding.Pathfinders;
using Content.Server.CPUJob.JobQueues;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.AI.Steering
{
    public sealed class NPCSteeringSystem : EntitySystem
    {
        // http://www.red3d.com/cwr/papers/1999/gdc99steer.html for a steering overview
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly PathfindingSystem _pathfindingSystem = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

        private bool _enabled;

        public override void Initialize()
        {
            base.Initialize();
            _configManager.OnValueChanged(CCVars.NPCEnabled, SetNPCEnabled, true);
        }

        private void SetNPCEnabled(bool obj)
        {
            if (!obj)
            {
                foreach (var comp in EntityQuery<NPCSteeringComponent>())
                {
                    comp.LastInput = Vector2.Zero;
                }
            }

            _enabled = obj;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _configManager.UnsubValueChanged(CCVars.NPCEnabled, SetNPCEnabled);
        }

        /// <summary>
        /// Adds the AI to the steering system to move towards a specific target
        /// </summary>
        public void Register(EntityUid entity, EntityCoordinates coordinates)
        {
            NPCSteeringComponent? comp;

            if (TryComp(entity, out comp))
            {
                comp.PathfindToken?.Cancel();
                comp.PathfindToken = null;
                comp.CurrentPath.Clear();
                comp.LastInput = Vector2.Zero;
            }
            else
            {
                comp = AddComp<NPCSteeringComponent>(entity);
            }

            comp.Coordinates = coordinates;
        }

        /// <summary>
        /// Stops the steering behavior for the AI and cleans up
        /// </summary>
        /// <param name="entity"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Unregister(NPCSteeringComponent component)
        {
            if (EntityManager.TryGetComponent(component.Owner, out InputMoverComponent? controller))
            {
                controller.CurTickSprintMovement = Vector2.Zero;
            }

            component.PathfindToken?.Cancel();
            component.PathfindToken = null;
            component.Pathfind = null;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!_enabled)
                return;

            foreach (var (steering, _, mover, xform) in EntityQuery<NPCSteeringComponent, ActiveNPCComponent, InputMoverComponent, TransformComponent>())
            {
                Steer(steering, mover, xform, frameTime);
            }
        }

        private void SetDirection(InputMoverComponent component, Vector2 value)
        {
            component.CurTickSprintMovement = value;
            component.LastInputTick = _timing.CurTick;
            component.LastInputSubTick = ushort.MaxValue;
        }

        /// <summary>
        /// Go through each steerer and combine their vectors
        /// </summary>
        private void Steer(NPCSteeringComponent steering, InputMoverComponent mover, TransformComponent xform, float frameTime)
        {
            if (!mover.CanMove ||
                xform.GridUid == null)
            {
                SetDirection(mover, Vector2.Zero);
                return;
            }

            // If we were pathfinding then try to update our path.
            if (steering.Pathfind != null)
            {
                switch (steering.Pathfind.Status)
                {
                    case JobStatus.Waiting:
                    case JobStatus.Running:
                    case JobStatus.Pending:
                    case JobStatus.Paused:
                        break;
                    case JobStatus.Finished:
                        steering.CurrentPath.Clear();

                        if (steering.Pathfind.Result != null)
                        {
                            foreach (var node in steering.Pathfind.Result)
                            {
                                steering.CurrentPath.Enqueue(node);
                            }
                        }

                        steering.Pathfind = null;
                        steering.PathfindToken = null;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // Although we may want to re-use our last vector2 input we don't want to overshoot our destination.
            // TODO: Steering behaviours.
        }

        /// <summary>
        /// Get a new job from the pathfindingsystem
        /// </summary>
        private void RequestPath(NPCSteeringComponent component, TransformComponent xform, PhysicsComponent? body)
        {
            if (component.PathfindToken != null)
            {
                return;
            }

            if (xform.GridUid == null)
                return;

            component.PathfindToken = new CancellationTokenSource();
            var gridManager = _mapManager.GetGrid(xform.GridUid.Value);
            var startTile = gridManager.GetTileRef(xform.Coordinates);
            var endTile = gridManager.GetTileRef(steeringRequest.TargetGrid);
            var collisionMask = 0;

            if (body != null)
            {
                collisionMask = body.CollisionMask;
            }

            var access = _accessReader.FindAccessTags(component.Owner);

            component.Pathfind = _pathfindingSystem.RequestPath(new PathfindingArgs(
                component.Owner,
                access,
                collisionMask,
                startTile,
                endTile,
                component.PathfindingProximity
            ), component.PathfindToken.Token);
        }

        #region Steering
        /// <summary>
        /// Move straight to target position
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="grid"></param>
        /// <returns></returns>
        private Vector2 Seek(EntityUid entity, EntityCoordinates grid)
        {
            // is-even much
            var entityPos = EntityManager.GetComponent<TransformComponent>(entity).Coordinates;
            return entityPos == grid
                ? Vector2.Zero
                : (grid.Position - entityPos.Position).Normalized;
        }

        /// <summary>
        /// Like Seek but slows down when within distance
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="grid"></param>
        /// <param name="slowingDistance"></param>
        /// <returns></returns>
        private Vector2 Arrival(EntityUid entity, EntityCoordinates grid, float slowingDistance = 1.0f)
        {
            var entityPos = EntityManager.GetComponent<TransformComponent>(entity).Coordinates;
            DebugTools.Assert(slowingDistance > 0.0f);
            if (entityPos == grid)
            {
                return Vector2.Zero;
            }
            var targetDiff = grid.Position - entityPos.Position;
            var rampedSpeed = targetDiff.Length / slowingDistance;
            return targetDiff.Normalized * MathF.Min(1.0f, rampedSpeed);
        }

        /// <summary>
        /// Like Seek but predicts target's future position
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private Vector2 Pursuit(EntityUid entity, EntityUid target)
        {
            var entityPos = EntityManager.GetComponent<TransformComponent>(entity).Coordinates;
            var targetPos = EntityManager.GetComponent<TransformComponent>(target).Coordinates;
            if (entityPos == targetPos)
            {
                return Vector2.Zero;
            }

            if (EntityManager.TryGetComponent(target, out IPhysBody? physics))
            {
                var targetDistance = (targetPos.Position - entityPos.Position);
                targetPos = targetPos.Offset(physics.LinearVelocity * targetDistance);
            }

            return (targetPos.Position - entityPos.Position).Normalized;
        }

        #endregion
    }

    public enum SteeringStatus
    {
        Pending,
        NoPath,
        Arrived,
        Moving,
    }
}
