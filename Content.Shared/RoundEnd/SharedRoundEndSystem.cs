using System.Threading;

namespace Content.Shared.RoundEnd
{
    public abstract class SharedRoundEndSystem : EntitySystem
    {
        public abstract bool IsRoundEndRequested();
    }
}