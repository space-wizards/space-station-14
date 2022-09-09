using Robust.Shared.Timing;

namespace Content.Server.CPUJob.JobQueues.Queues
{
    [Virtual]
    public class JobQueue
    {
        private readonly IStopwatch _stopwatch;

        public JobQueue() : this(new Stopwatch()) {}

        public JobQueue(IStopwatch stopwatch)
        {
            _stopwatch = stopwatch;
        }

        /// <summary>
        /// How long the job's allowed to run for before suspending
        /// </summary>
        public virtual double MaxTime { get; } = 0.002;

        private readonly Queue<IJob> _pendingQueue = new();
        private readonly List<IJob> _waitingJobs = new();

        public void EnqueueJob(IJob job)
        {
            _pendingQueue.Enqueue(job);
        }

        public void Process()
        {
            // Move all finished waiting jobs back into the regular queue.
            foreach (var waitingJob in _waitingJobs)
            {
                if (waitingJob.Status != JobStatus.Waiting)
                {
                    _pendingQueue.Enqueue(waitingJob);
                }
            }

            _waitingJobs.RemoveAll(p => p.Status != JobStatus.Waiting);

            // At one point I tried making the pathfinding queue multi-threaded but ehhh didn't go great
            // Could probably try it again at some point
            // it just seemed slow af but I was probably doing something dumb with semaphores
            _stopwatch.Restart();

            // Although the jobs can stop themselves we might be able to squeeze more of them in the allotted time
            while (_stopwatch.Elapsed.TotalSeconds < MaxTime && _pendingQueue.TryDequeue(out var job))
            {
                // Deque and re-enqueue these to cycle them through to avoid starvation if we've got a lot of jobs.

                job.Run();

                switch (job.Status)
                {
                    case JobStatus.Finished:
                        continue;
                    case JobStatus.Waiting:
                        // If this job goes into waiting we have to move it into a separate list.
                        // Otherwise we'd just be spinning like mad here for external IO or such.
                        _waitingJobs.Add(job);
                        break;
                    default:
                        _pendingQueue.Enqueue(job);
                        break;
                }
            }
        }
    }
}
