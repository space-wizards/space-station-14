using System.Net;
using Content.Shared.Database;
using Content.Shared.Eui;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

[Serializable, NetSerializable]
public sealed class BanPanelEuiState(string playerName, bool hasBan) : EuiStateBase
{
    public string PlayerName { get; set; } = playerName;
    public bool HasBan { get; set; } = hasBan;
}

public static class BanPanelEuiStateMsg
{
    [Serializable, NetSerializable]
    public sealed class CreateBanRequest(
        string? player,
        (IPAddress, int)? ipAddress,
        bool useLastIp,
        ImmutableTypedHwid? hwid,
        bool useLastHwid,
        uint minutes,
        string reason,
        NoteSeverity severity,
        ProtoId<JobPrototype>[]? jobs,
        ProtoId<AntagPrototype>[]? antags,
        bool erase
    ) : EuiMessageBase
    {
        public string? Player { get; set; } = player;

        public string? IpAddress { get; set; } =
            ipAddress == null ? null : $"{ipAddress.Value.Item1}/{ipAddress.Value.Item2}";

        public ImmutableTypedHwid? Hwid { get; set; } = hwid;
        public uint Minutes { get; set; } = minutes;
        public string Reason { get; set; } = reason;
        public NoteSeverity Severity { get; set; } = severity;
        public ProtoId<JobPrototype>[]? Jobs { get; set; } = jobs;
        public ProtoId<AntagPrototype>[]? Antags { get; set; } = antags;
        public bool UseLastIp { get; set; } = useLastIp;
        public bool UseLastHwid { get; set; } = useLastHwid;
        public bool Erase { get; set; } = erase;
    }

    [Serializable, NetSerializable]
    public sealed class GetPlayerInfoRequest(string username) : EuiMessageBase
    {
        public string PlayerUsername { get; set; } = username;
    }
}
