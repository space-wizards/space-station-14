using System.Collections.Generic;
using Robust.Shared.Timing;

namespace Content.Server.GameObjects.EntitySystems.JobQueues.Queues
{
    public class JobQueue
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();

        /// <summary>
        /// How long the job's allowed to run for before suspending
        /// </summary>
        public virtual double MaxTime => 0.002;
        public Queue<IJob> PendingQueue { get; } = new Queue<IJob>();

        public void Process()
        {
            // At one point I tried making the pathfinding queue multi-threaded but ehhh didn't go great
            // Could probably try it again at some point
            // it just seemed slow af but I was probably doing something dumb with semaphores
            _stopwatch.Restart();

            // Although the jobs can stop themselves we might be able to squeeze more of them in the allotted time
            while (PendingQueue.Count > 0 && _stopwatch.Elapsed.TotalSeconds < MaxTime)
            {
                var job = PendingQueue.Peek();
                job.Run();
                if (job.Status == Status.Finished)
                {
                    PendingQueue.Dequeue();
                }
            }
        }
    }
}
