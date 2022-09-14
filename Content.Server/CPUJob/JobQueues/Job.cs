using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.CPUJob.JobQueues
{
    /// <summary>
    ///     CPU-intensive job that can be suspended and resumed on the main thread
    /// </summary>
    /// <remarks>
    ///     Implementations should overload <see cref="Process"/>.
    ///     Inside <see cref="Process"/>, implementations should only await on <see cref="SuspendNow"/>,
    ///     <see cref="SuspendIfOutOfTime"/>, or <see cref="WaitAsyncTask"/>.
    /// </remarks>
    /// <typeparam name="T">The type of result this job generates</typeparam>
    public abstract class Job<T> : IJob
    {
        public JobStatus Status { get; private set; } = JobStatus.Pending;

        /// <summary>
        ///     Represents the status of this job as a regular task.
        /// </summary>
        public Task<T?> AsTask { get; }

        public T? Result { get; private set; }
        public Exception? Exception { get; private set; }
        protected CancellationToken Cancellation { get; }

        public double DebugTime { get; private set; }
        private readonly double _maxTime;
        protected readonly IStopwatch StopWatch;

        // TCS for the Task property.
        private readonly TaskCompletionSource<T?> _taskTcs;

        // TCS to call to resume the suspended job.
        private TaskCompletionSource<object?>? _resume;
        private Task? _workInProgress;

        protected Job(double maxTime, CancellationToken cancellation = default)
            : this(maxTime, new Stopwatch(), cancellation)
        {
        }

        protected Job(double maxTime, IStopwatch stopwatch, CancellationToken cancellation = default)
        {
            _maxTime = maxTime;
            StopWatch = stopwatch;
            Cancellation = cancellation;

            _taskTcs = new TaskCompletionSource<T?>();
            AsTask = _taskTcs.Task;
        }

        /// <summary>
        ///     Suspends the current task immediately, yielding to other running jobs.
        /// </summary>
        /// <remarks>
        ///     This does not stop the job queue from un-suspending the current task immediately again,
        ///     if there is still time left over.
        /// </remarks>
        protected Task SuspendNow()
        {
            DebugTools.AssertNull(_resume);

            _resume = new TaskCompletionSource<object?>();
            Status = JobStatus.Paused;
            DebugTime += StopWatch.Elapsed.TotalSeconds;
            return _resume.Task;
        }

        protected ValueTask SuspendIfOutOfTime()
        {
            DebugTools.AssertNull(_resume);

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (StopWatch.Elapsed.TotalSeconds <= _maxTime || _maxTime == 0.0)
            {
                return new ValueTask();
            }

            return new ValueTask(SuspendNow());
        }

        /// <summary>
        ///     Wrapper to await on an external task.
        /// </summary>
        protected async Task<TTask> WaitAsyncTask<TTask>(Task<TTask> task)
        {
            DebugTools.AssertNull(_resume);

            Status = JobStatus.Waiting;
            DebugTime += StopWatch.Elapsed.TotalSeconds;

            var result = await task;

            // Immediately block on resume so that everything stays correct.
            Status = JobStatus.Paused;
            _resume = new TaskCompletionSource<object?>();

            await _resume.Task;

            return result;
        }

        /// <summary>
        ///     Wrapper to safely await on an external task.
        /// </summary>
        protected async Task WaitAsyncTask(Task task)
        {
            DebugTools.AssertNull(_resume);

            Status = JobStatus.Waiting;
            DebugTime += StopWatch.Elapsed.TotalSeconds;

            await task;

            // Immediately block on resume so that everything stays correct.
            _resume = new TaskCompletionSource<object?>();
            Status = JobStatus.Paused;

            await _resume.Task;
        }

        public void Run()
        {
            StopWatch.Restart();
            _workInProgress ??= ProcessWrap();

            if (Status == JobStatus.Finished)
            {
                return;
            }

            DebugTools.Assert(_resume != null,
                "Run() called without resume. Was this called while the job is in Waiting state?");
            var resume = _resume;
            _resume = null;

            Status = JobStatus.Running;

            if (Cancellation.IsCancellationRequested)
            {
                resume?.TrySetCanceled();
            }
            else
            {
                resume?.SetResult(null);
            }

            if (Status != JobStatus.Finished && Status != JobStatus.Waiting)
            {
                DebugTools.Assert(_resume != null,
                    "Job suspended without _resume set. Did you await on an external task without using WaitAsyncTask?");
            }
        }

        protected abstract Task<T?> Process();

        private async Task ProcessWrap()
        {
            try
            {
                Cancellation.ThrowIfCancellationRequested();

                // Making sure that the task starts inside the Running block,
                // where the stopwatch is correctly set and such.
                await SuspendNow();
                Result = await Process();

                // TODO: not sure if it makes sense to connect Task directly up
                // to the return value of this method/Process.
                // Maybe?
                _taskTcs.TrySetResult(Result);
            }
            catch (OperationCanceledException)
            {
                _taskTcs.TrySetCanceled();
            }
            catch (Exception e)
            {
                // TODO: Should this be exposed differently?
                // I feel that people might forget to check whether the job failed.
                Logger.ErrorS("job", "Job failed on exception:\n{0}", e);
                Exception = e;
                _taskTcs.TrySetException(e);
            }
            finally
            {
                if (Status != JobStatus.Waiting)
                {
                    // If we're blocked on waiting and the waiting task goes cancel/exception,
                    // this timing info would not be correct.
                    DebugTime += StopWatch.Elapsed.TotalSeconds;
                }
                Status = JobStatus.Finished;
            }
        }
    }

    public enum JobStatus
    {
        /// <summary>
        ///     Job has been created and has not been ran yet.
        /// </summary>
        Pending,

        /// <summary>
        ///     Job is currently (yes, right now!) executing.
        /// </summary>
        Running,

        /// <summary>
        ///     Job is paused due to CPU limits.
        /// </summary>
        Paused,

        /// <summary>
        ///     Job is paused because of waiting on external task.
        /// </summary>
        Waiting,

        /// <summary>
        ///     Job is done.
        /// </summary>
        // TODO: Maybe have a different status code for cancelled/failed on exception?
        Finished,
    }
}
