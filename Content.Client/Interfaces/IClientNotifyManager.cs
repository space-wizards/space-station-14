using Content.Shared.Interfaces;
using Robust.Client;
using Robust.Shared.Map;

namespace Content.Client.Interfaces
{
    public interface IClientNotifyManager : ISharedNotifyManager
    {
        void Initialize();
        void PopupMessage(ScreenCoordinates coordinates, string message);
        void PopupMessage(string message);
        void FrameUpdate(RenderFrameEventArgs eventArgs);
    }
}
