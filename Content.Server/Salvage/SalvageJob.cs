using System.Threading;
using System.Threading.Tasks;
using Content.Server.CPUJob.JobQueues;
using Robust.Shared.Timing;

namespace Content.Server.Salvage;

public sealed class SalvageJob : Job<EntityUid>
{
    public SalvageJob(double maxTime, CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
    }

    public SalvageJob(double maxTime, IStopwatch stopwatch, CancellationToken cancellation = default) : base(maxTime, stopwatch, cancellation)
    {
    }

    protected override Task<EntityUid> Process()
    {
        throw new NotImplementedException();
    }
}
