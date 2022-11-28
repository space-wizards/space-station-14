using System.Threading;
using System.Threading.Tasks;
using Content.Server.CPUJob.JobQueues;
using Robust.Shared.Timing;

namespace Content.Server.Salvage;

public sealed class SalvageJob : Job<EntityUid>
{
    private Random _random;

    public SalvageJob(int seed, double maxTime, CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
        _random = new Random(seed);
    }

    protected override Task<EntityUid> Process()
    {
        throw new NotImplementedException();
    }
}
