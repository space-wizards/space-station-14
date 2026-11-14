using System.Buffers;
using Robust.Shared.Threading;

namespace Content.Server.Radiation.Systems;

public partial class RadiationSystem
{
    private sealed class RadiationJob : IParallelBulkRobustJob
    {
        private readonly RadiationSystem _system;
        private readonly ReceiverSnapshot[] _receivers;
        private readonly float[] _results;

        public int BatchSize => 5;

        public RadiationJob(RadiationSystem system, ReceiverSnapshot[] receivers, float[] results)
        {
            _system = system;
            _receivers = receivers;
            _results = results;
        }

        public void ExecuteRange(int startIndex, int endIndex)
        {
            var nearbySources = ArrayPool<int>.Shared.Rent(_system._sourceSnapshots.Length);

            try
            {
                for (var i = startIndex; i < endIndex; i++)
                    _results[i] = _system.CalculateRadiation(_receivers[i], nearbySources, false, null);
            }
            finally
            {
                ArrayPool<int>.Shared.Return(nearbySources);
            }
        }
    }
}
