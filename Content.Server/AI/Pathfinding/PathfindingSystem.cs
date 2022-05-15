using System.Threading;
using Content.Server.Access;
using Content.Server.AI.Pathfinding.Pathfinders;
using Content.Server.CPUJob.JobQueues;
using Content.Server.CPUJob.JobQueues.Queues;
using Content.Shared.Access.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Content.Server.AI.Pathfinding
{
    /// <summary>
    /// This system handles pathfinding graph updates as well as dispatches to the pathfinder
    /// (90% of what it's doing is graph updates so not much point splitting the 2 roles)
    /// </summary>
    public sealed partial class PathfindingSystem : EntitySystem
    {
        private readonly PathfindingJobQueue _pathfindingQueue = new();

        public const int TrackedCollisionLayers = (int)
            (CollisionGroup.Impassable |
             CollisionGroup.MidImpassable |
             CollisionGroup.LowImpassable |
             CollisionGroup.HighImpassable);

        /// <summary>
        /// Ask for the pathfinder to gimme somethin
        /// </summary>
        /// <param name="pathfindingArgs"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Job<Queue<TileRef>> RequestPath(PathfindingArgs pathfindingArgs, CancellationToken cancellationToken)
        {
            var startNode = GetNode(pathfindingArgs.Start);
            var endNode = GetNode(pathfindingArgs.End);
            var job = new AStarPathfindingJob(0.003, startNode, endNode, pathfindingArgs, cancellationToken);
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
