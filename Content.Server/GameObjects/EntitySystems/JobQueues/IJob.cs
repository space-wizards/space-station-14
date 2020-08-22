using System.Collections;

namespace Content.Server.GameObjects.EntitySystems.JobQueues
{
    public interface IJob
    {
        JobStatus Status { get; }
        void Run();
    }
}