namespace Content.Server.CPUJob.JobQueues
{
    public interface IJob
    {
        JobStatus Status { get; }
        void Run();
    }
}
