using Content.Shared.Notification;
using Content.Shared.Notification.Managers;

namespace Content.Server.Notification.Managers
{
    public interface IServerNotifyManager : ISharedNotifyManager
    {
        void Initialize();
    }
}
