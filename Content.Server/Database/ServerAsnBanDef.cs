using Content.Shared.Database;
using Robust.Shared.Network;

namespace Content.Server.Database
{
    public sealed class ServerAsnBanDef
    {
        public string Asn { get; }
        public NetUserId? BanningAdmin { get; }
        public DateTimeOffset BanTime { get; }
        public DateTimeOffset? ExpirationTime { get; }
        public NoteSeverity Severity { get; }
        public string Reason { get; }
        public ServerAsnUnbanDef? Unban { get;  }

        public ServerAsnBanDef(string asn,
            NetUserId? banningAdmin,
            DateTimeOffset banTime,
            DateTimeOffset? expirationTime,
            NoteSeverity severity,
            string reason,
            ServerAsnUnbanDef? unban)
        {
            Asn = asn;
            BanningAdmin = banningAdmin;
            BanTime = banTime;
            ExpirationTime = expirationTime;
            Severity = severity;
            Reason = reason;
            Unban = unban;
        }
    }
}
