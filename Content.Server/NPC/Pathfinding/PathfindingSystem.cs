using System.Threading;
using Content.Server.CPUJob.JobQueues;
using Content.Server.CPUJob.JobQueues.Queues;
using Content.Server.NPC.Pathfinding.Pathfinders;
using Content.Shared.Access.Systems;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Server.NPC.Pathfinding
{
    /// <summary>
    /// This system handles pathfinding graph updates as well as dispatches to the pathfinder
    /// (90% of what it's doing is graph updates so not much point splitting the 2 roles)
    /// </summary>
    public sealed partial class PathfindingSystem : EntitySystem
    {
        [Dependency] private readonly AccessReaderSystem _access = default!;

        private readonly PathfindingJobQueue _pathfindingQueue = new();

        public const int TrackedCollisionLayers = (int)
            (CollisionGroup.Impassable |
             CollisionGroup.MidImpassable |
             CollisionGroup.LowImpassable |
             CollisionGroup.HighImpassable);

        /// <summary>
        /// Ask for the pathfinder to gimme somethin
        /// </summary>
        public Job<Queue<TileRef>> RequestPath(PathfindingArgs pathfindingArgs, CancellationToken cancellationToken)
        {
            var startNode = GetNode(pathfindingArgs.Start);
            var endNode = GetNode(pathfindingArgs.End);
            var job = new AStarPathfindingJob(0.001, startNode, endNode, pathfindingArgs, cancellationToken, EntityManager);
            _pathfindingQueue.EnqueueJob(job);

            return job;
        }

        public Job<Queue<TileRef>>? RequestPath(EntityUid source, EntityUid target, CancellationToken cancellationToken)
        {
            var collisionMask = 0;

            if (TryComp<PhysicsComponent>(source, out var body))
            {
                collisionMask = body.CollisionMask;
            }

            if (!TryComp<TransformComponent>(source, out var xform) ||
                !_mapManager.TryGetGrid(xform.GridUid, out var grid) ||
                !TryComp<TransformComponent>(target, out var targetXform) ||
                !_mapManager.TryGetGrid(targetXform.GridUid, out var targetGrid))
            {
                return null;
            }

            var start = grid.GetTileRef(xform.Coordinates);
            var end = targetGrid.GetTileRef(targetXform.Coordinates);

            var args = new PathfindingArgs(source, _access.FindAccessTags(source), collisionMask, start, end);

            var startNode = GetNode(start);
            var endNode = GetNode(end);
            var job = new AStarPathfindingJob(0.001, startNode, endNode, args, cancellationToken, EntityManager);
            _pathfindingQueue.EnqueueJob(job);

            return job;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            ProcessGridUpdates();
            _pathfindingQueue.Process();
        }
    }
}
