using System.Collections;

namespace Content.Server.GameObjects.EntitySystems.JobQueues
{
    public interface IJob
    {
        Status Status { get; }
        void Run();
    }
}