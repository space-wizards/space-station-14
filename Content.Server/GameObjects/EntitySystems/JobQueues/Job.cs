using System.Collections;
using Robust.Shared.Timing;

namespace Content.Server.GameObjects.EntitySystems.JobQueues
{
    public abstract class Job<T> : IJob
    {
        protected double DebugTime;
        private readonly double _maxTime;
        public Status Status { get; protected set; } = Status.Pending;

        protected readonly Stopwatch StopWatch = new Stopwatch();

        public T Result { get; protected set; }
        private IEnumerator _workInProgress = null;

        protected Job(double maxTime)
        {
            _maxTime = maxTime;
        }

        protected bool OutOfTime()
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (StopWatch.Elapsed.TotalSeconds < _maxTime || _maxTime == 0.0)
            {
                return false;
            }

            DebugTime += StopWatch.Elapsed.TotalSeconds;
            Status = Status.Paused;
            return true;
        }

        public void Run()
        {
            if (_workInProgress == null)
            {
                _workInProgress = Process();
            }

            if (Status == Status.Finished)
            {
                return;
            }

            Status = Status.Running;
            StopWatch.Restart();
            _workInProgress.MoveNext();
        }

        /// <summary>
        /// Functions as a Coroutine
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerator Process();

        protected void Finish()
        {
            DebugTime += StopWatch.Elapsed.TotalSeconds;
            Status = Status.Finished;
        }
    }

    public enum Status
    {
        Pending,
        Running,
        Paused,
        Finished,
    }
}
