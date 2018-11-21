using Content.Shared.Interfaces;

namespace Content.Server.Interfaces
{
    public interface IServerNotifyManager : ISharedNotifyManager
    {
        void Initialize();
    }
}
