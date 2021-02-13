using System;
using System.Net;
using Robust.Shared.Network;

#nullable enable

namespace Content.Server.Database
{
    public sealed class ServerBanDef
    {
        public int? Id { get; }
        public NetUserId? UserId { get; }
        public (IPAddress address, int cidrMask)? Address { get; }

        public DateTimeOffset BanTime { get; }
        public DateTimeOffset? ExpirationTime { get; }
        public string Reason { get; }
        public NetUserId? BanningAdmin { get; }

        public ServerBanDef(int? id, NetUserId? userId, (IPAddress, int)? address, DateTimeOffset banTime, DateTimeOffset? expirationTime, string reason, NetUserId? banningAdmin)
        {
            if (userId == null && address == null)
            {
                throw new ArgumentException("Must have a banned user, banned address, or both.");
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
            BanTime = banTime;
            ExpirationTime = expirationTime;
            Reason = reason;
            BanningAdmin = banningAdmin;
        }
    }
}
