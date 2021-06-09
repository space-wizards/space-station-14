using Content.Shared.Notification;

namespace Content.Server.Notification.Managers
{
    public interface IServerNotifyManager : ISharedNotifyManager
    {
        void Initialize();
    }
}
