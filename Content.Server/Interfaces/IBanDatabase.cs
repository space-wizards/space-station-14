using System.Net;

namespace Content.Server.Interfaces
{
    public interface IBanDatabase
    {
        void StartInit();
        void FinishInit();
        string GetIpBan(IPAddress address);
        void BanIpAddress(IPAddress address, string reason);
        void UnbanIpAddress(IPAddress address);
    }
}
