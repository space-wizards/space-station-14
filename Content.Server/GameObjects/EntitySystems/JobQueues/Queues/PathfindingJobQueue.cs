namespace Content.Server.GameObjects.EntitySystems.JobQueues.Queues
{
    public sealed class PathfindingJobQueue : JobQueue
    {
        public override double MaxTime => 0.003;
    }
}
