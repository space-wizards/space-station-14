using System.Threading;

namespace Content.Shared.RoundEnd
{
    public abstract class SharedRoundEndSystem : EntitySystem
    {
        private CancellationTokenSource? _roundEndCountdownTokenSource = null;

        public void CancelCountdown()
        {
            if (_roundEndCountdownTokenSource != null)
            {
                _roundEndCountdownTokenSource.Cancel();
                _roundEndCountdownTokenSource = null;
            }
        }

        public CancellationTokenSource GetOrStartCountdown()
        {
            if (_roundEndCountdownTokenSource == null)
            {
                _roundEndCountdownTokenSource = new();
            }
            return _roundEndCountdownTokenSource;
        }

        public bool IsRoundEndRequested()
        {
            return _roundEndCountdownTokenSource != null;
        }
    }
}