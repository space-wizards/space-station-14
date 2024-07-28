using Content.Shared.Database;
using Robust.Shared.Network;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.Administration.Managers
{
    public class BanEventArgs
    {
        public class ServerBanEventArgs : EventArgs
        {
            public NetUserId? Target { get; }
            public string? TargetUsername { get; }
            public NetUserId? BanningAdmin { get; }
            public (IPAddress, int)? AddressRange { get; }
            public ImmutableArray<byte>? Hwid { get; }
            public uint? Minutes { get; }
            public NoteSeverity Severity { get; }
            public string Reason { get; }

            public ServerBanEventArgs(NetUserId? target, string? targetUsername, NetUserId? banningAdmin, (IPAddress, int)? addressRange, ImmutableArray<byte>? hwid, uint? minutes, NoteSeverity severity, string reason)
            {
                Target = target;
                TargetUsername = targetUsername;
                BanningAdmin = banningAdmin;
                AddressRange = addressRange;
                Hwid = hwid;
                Minutes = minutes;
                Severity = severity;
                Reason = reason;
            }
        }
    }
}
