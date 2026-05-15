using Robust.Shared.CPUJob.JobQueues.Queues;

namespace Content.Server.CPUJob.JobQueues.Queues
{
    public sealed class PathfindingJobQueue : JobQueue
    {
        public override double MaxTime => 0.003;
    }
}
