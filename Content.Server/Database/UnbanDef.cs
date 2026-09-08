using Robust.Shared.Network;

namespace Content.Server.Database
{
    public sealed class UnbanDef
    {
        public int BanId { get; }

        public NetUserId? UnbanningAdmin { get; }

        public DateTimeOffset UnbanTime { get; }

        public UnbanDef(int banId, NetUserId? unbanningAdmin, DateTimeOffset unbanTime)
        {
            BanId = banId;
            UnbanningAdmin = unbanningAdmin;
            UnbanTime = unbanTime;
        }
    }
}
