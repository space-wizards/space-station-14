using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Parallax;

public sealed class NewBiomeSystem : EntitySystem
{
    /*
     * Handles loading in biomes around players.
     * Separate but similar to dungeons.
     */

    /// <summary>
    /// Jobs for biomes to load.
    /// </summary>
    private JobQueue _biomeQueue = new(0.005);

    private Dictionary<ICommonSession, BiomeLoadJob> _jobs = new();

    public override void Initialize()
    {
        base.Initialize();

        // TODO: Cvar to control biome allowed load time.
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Get all relevant players.
        // If they already have a job loading then don't make a new one yet.
        foreach (var player in new List<ICommonSession>())
        {
            // If not relevant then discard.
        }

        // Process jobs.
        _biomeQueue.Process();
    }

    private sealed class BiomeLoadJob : Job<bool>
    {
        /// <summary>
        /// Bounds to load in. The actual area may be loaded larger due to layer dependencies.
        /// </summary>
        public Box2i Bounds;

        public BiomeLoadJob(double maxTime, CancellationToken cancellation = default) : base(maxTime, cancellation)
        {
        }

        public BiomeLoadJob(double maxTime, IStopwatch stopwatch, CancellationToken cancellation = default) : base(maxTime, stopwatch, cancellation)
        {
        }

        protected override Task<bool> Process()
        {
            throw new NotImplementedException();
        }
    }
}
