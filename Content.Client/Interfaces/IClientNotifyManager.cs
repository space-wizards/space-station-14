using Content.Shared.Interfaces;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Interfaces
{
    public interface IClientNotifyManager : ISharedNotifyManager
    {
        void Initialize();
        void PopupMessage(ScreenCoordinates coordinates, string message);
        void PopupMessage(string message);
        void FrameUpdate(FrameEventArgs eventArgs);
    }
}
