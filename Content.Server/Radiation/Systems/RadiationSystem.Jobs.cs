using System.Buffers;
using Content.Shared.Radiation.Systems;
using Robust.Shared.Threading;

namespace Content.Server.Radiation.Systems;

public partial class RadiationSystem
{
    private sealed class RadiationJob : IParallelBulkRobustJob
    {
        private const int JobBatchSize = 5;

        private readonly RadiationSystem _system;
        private readonly ReceiverSnapshot[] _receivers;
        private readonly float[] _results;
        private readonly List<DebugRadiationRay>[]? _debugRays;

        public int BatchSize => JobBatchSize;

        public RadiationJob(
            RadiationSystem system,
            ReceiverSnapshot[] receivers,
            float[] results,
            List<DebugRadiationRay>[]? debugRays)
        {
            _system = system;
            _receivers = receivers;
            _results = results;
            _debugRays = debugRays;
        }

        public void ExecuteRange(int startIndex, int endIndex)
        {
            var nearbySources = ArrayPool<int>.Shared.Rent(_system._sourceCount);

            try
            {
                for (var i = startIndex; i < endIndex; i++)
                {
                    _results[i] = _system.CalculateRadiation(
                        _receivers[i],
                        nearbySources,
                        _debugRays?[i]);
                }
            }
            finally
            {
                ArrayPool<int>.Shared.Return(nearbySources);
            }
        }
    }
}
