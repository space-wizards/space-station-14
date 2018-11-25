using SS14.Client;

namespace Content.Client.Interfaces
{
    public interface IClientGameTicker
    {
        void Initialize();
        void FrameUpdate(RenderFrameEventArgs renderFrameEventArgs);
    }
}
