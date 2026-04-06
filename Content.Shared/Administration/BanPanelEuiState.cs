using System.Net;
using Content.Shared.Database;
using Content.Shared.Eui;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
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
    public sealed class CreateBanRequest(Ban ban) : EuiMessageBase
    {
        public Ban Ban { get; } = ban;
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

/// <summary>
///     Contains all the data related to a particular ban action created by the BanPanel window.
/// </summary>
[Serializable, NetSerializable]
public sealed record Ban
{
    public Ban(
        string? target,
        (IPAddress, int)? ipAddressTuple,
        bool useLastIp,
        ImmutableTypedHwid? hwid,
        bool useLastHwid,
        uint banDurationMinutes,
        string reason,
        NoteSeverity severity,
        ProtoId<JobPrototype>[]? bannedJobs,
        ProtoId<AntagPrototype>[]? bannedAntags,
        bool erase)
    {
        Target = target;
        IpAddress = ipAddressTuple?.Item1.ToString();
        IpAddressHid = ipAddressTuple?.Item2.ToString() ?? "0";
        UseLastIp = useLastIp;
        Hwid = hwid;
        UseLastHwid = useLastHwid;
        BanDurationMinutes = banDurationMinutes;
        Reason = reason;
        Severity = severity;
        BannedJobs = bannedJobs;
        BannedAntags = bannedAntags;
        Erase = erase;
    }

    public readonly string? Target;
    public readonly string? IpAddress;
    public readonly string? IpAddressHid;
    public readonly bool UseLastIp;
    public readonly ImmutableTypedHwid? Hwid;
    public readonly bool UseLastHwid;
    public readonly uint BanDurationMinutes;
    public readonly string Reason;
    public readonly NoteSeverity Severity;
    public readonly ProtoId<JobPrototype>[]? BannedJobs;
    public readonly ProtoId<AntagPrototype>[]? BannedAntags;
    public readonly bool Erase;
}
