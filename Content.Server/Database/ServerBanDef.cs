using System.Collections.Immutable;
using System.Net;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Network;


namespace Content.Server.Database
{
    public sealed class ServerBanDef
    {
        public int? Id { get; }
        public NetUserId? UserId { get; }
        public (IPAddress address, int cidrMask)? Address { get; }
        public ImmutableArray<byte>? HWId { get; }

        public DateTimeOffset BanTime { get; }
        public DateTimeOffset? ExpirationTime { get; }
        public string Reason { get; }
        public NetUserId? BanningAdmin { get; }
        public ServerUnbanDef? Unban { get; }

        public ServerBanDef(
            int? id,
            NetUserId? userId,
            (IPAddress, int)? address,
            ImmutableArray<byte>? hwId,
            DateTimeOffset banTime,
            DateTimeOffset? expirationTime,
            string reason,
            NetUserId? banningAdmin,
            ServerUnbanDef? unban)
        {
            if (userId == null && address == null && hwId ==  null)
            {
                throw new ArgumentException("Must have at least one of banned user, banned address or hardware ID");
            }

            if (address is {} addr && addr.Item1.IsIPv4MappedToIPv6)
            {
                // Fix IPv6-mapped IPv4 addresses
                // So that IPv4 addresses are consistent between separate-socket and dual-stack socket modes.
                address = (addr.Item1.MapToIPv4(), addr.Item2 - 96);
            }

            Id = id;
            UserId = userId;
            Address = address;
            HWId = hwId;
            BanTime = banTime;
            ExpirationTime = expirationTime;
            Reason = reason;
            BanningAdmin = banningAdmin;
            Unban = unban;
        }

        public string FormatBanMessage(IConfigurationManager cfg, ILocalizationManager loc)
        {
            string expires;
            if (ExpirationTime is { } expireTime)
            {
                var duration = expireTime - BanTime;
                var utc = expireTime.ToUniversalTime();
                expires = loc.GetString("ban-expires", ("duration", duration.TotalMinutes.ToString("N0")), ("time", utc.ToString("f")));
            }
            else
            {
                var appeal = cfg.GetCVar(CCVars.InfoLinksAppeal);
                if (!string.IsNullOrWhiteSpace(appeal))
                    expires = loc.GetString("ban-banned-permanent-appeal", ("link", appeal));
                else
                    expires = loc.GetString("ban-banned-permanent");
            }

            return $"""
                   {loc.GetString("ban-banned-1")}
                   {loc.GetString("ban-banned-2", ("reason", Reason))}
                   {expires}
                   {loc.GetString("ban-banned-3")}
                   """;
        }
    }
}
