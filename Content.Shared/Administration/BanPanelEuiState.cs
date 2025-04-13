using System.Net;
using Content.Shared.Database;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

[Serializable, NetSerializable]
public sealed class BanPanelEuiState : EuiStateBase
{
    public string PlayerName { get; set; }
    public bool HasBan { get; set; }

    public BanPanelEuiState(string playerName, bool hasBan)
    {
        PlayerName = playerName;
        HasBan = hasBan;
    }
}

public static class BanPanelEuiStateMsg
{
    [Serializable, NetSerializable]
    public sealed class CreateBanRequest : EuiMessageBase
    {
        public string? Player { get; set; }
        public string? IpAddress { get; set; }
        public ImmutableTypedHwid? Hwid { get; set; }
        public uint Minutes { get; set; }
        public string Reason { get; set; }
        public NoteSeverity Severity { get; set; }
        public string[]? Roles { get; set; }
        public bool UseLastIp { get; set; }
        public bool UseLastHwid { get; set; }
        public bool Erase { get; set; }

        public CreateBanRequest(string? player, (IPAddress, int)? ipAddress, bool useLastIp, ImmutableTypedHwid? hwid, bool useLastHwid, uint minutes, string reason, NoteSeverity severity, string[]? roles, bool erase)
        {
            Player = player;
            IpAddress = ipAddress == null ? null : $"{ipAddress.Value.Item1}/{ipAddress.Value.Item2}";
            UseLastIp = useLastIp;
            Hwid = hwid;
            UseLastHwid = useLastHwid;
            Minutes = minutes;
            Reason = reason;
            Severity = severity;
            Roles = roles;
            Erase = erase;
        }
    }

    [Serializable, NetSerializable]
    public sealed class GetPlayerInfoRequest : EuiMessageBase
    {
        public string PlayerUsername { get; set; }

        public GetPlayerInfoRequest(string username)
        {
            PlayerUsername = username;
        }
    }
}
