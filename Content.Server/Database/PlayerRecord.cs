using System;
using System.Net;
using Robust.Shared.Network;

namespace Content.Server.Database
{
    public sealed class PlayerRecord
    {
        public NetUserId UserId { get; }
        public DateTimeOffset FirstSeenTime { get; }
        public string LastSeenUserName { get; }
        public DateTimeOffset LastSeenTime { get; }
        public IPAddress LastSeenAddress { get; }

        public PlayerRecord(
            NetUserId userId,
            DateTimeOffset firstSeenTime,
            string lastSeenUserName,
            DateTimeOffset lastSeenTime,
            IPAddress lastSeenAddress)
        {
            UserId = userId;
            FirstSeenTime = firstSeenTime;
            LastSeenUserName = lastSeenUserName;
            LastSeenTime = lastSeenTime;
            LastSeenAddress = lastSeenAddress;
        }
    }
}
