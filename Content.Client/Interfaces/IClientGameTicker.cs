using Robust.Shared.Timing;

namespace Content.Client.Interfaces
{
    public interface IClientGameTicker
    {
        void Initialize();
        void FrameUpdate(FrameEventArgs FrameEventArgs);
    }
}
