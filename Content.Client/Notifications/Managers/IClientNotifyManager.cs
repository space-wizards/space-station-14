using Content.Shared.Notification;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Notifications.Managers
{
    public interface IClientNotifyManager : ISharedNotifyManager
    {
        void Initialize();
        void PopupMessage(ScreenCoordinates coordinates, string message);
        void PopupMessage(string message);
        void FrameUpdate(FrameEventArgs eventArgs);
    }
}
