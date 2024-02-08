using System;
using System.Threading;

namespace Content.Shared.PlantAnalyzer
{
    [Serializable]
    public class CancellationTokenWrapper
    {
        [NonSerialized] private CancellationTokenSource _cancellationTokenSource;

        public CancellationToken Token => _cancellationTokenSource.Token;

        public CancellationTokenWrapper(CancellationTokenSource cancellationTokenSource)
        {
            _cancellationTokenSource = cancellationTokenSource;
        }
    }
}
