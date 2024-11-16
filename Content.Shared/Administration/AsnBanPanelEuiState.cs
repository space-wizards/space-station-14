using System.Net;
using Content.Shared.Database;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

[Serializable, NetSerializable]
public sealed class AsnBanPanelEuiState : EuiStateBase
{
    public bool HasBan { get; set; }

    public AsnBanPanelEuiState(bool hasBan)
    {
        HasBan = hasBan;
    }
}

public static class AsnBanPanelEuiStateMsg
{
    [Serializable, NetSerializable]
    public sealed class CreateAsnBanRequest : EuiMessageBase
    {
        public string Asn { get; set; }
        public uint? Minutes { get; set; }
        public string Reason { get; set; }
        public NoteSeverity Severity { get; set; }

        public CreateAsnBanRequest(string asn, uint? minutes, string reason, NoteSeverity severity)
        {
            Asn = asn;
            Minutes = minutes;
            Reason = reason;
            Severity = severity;
        }
    }

    [Serializable, NetSerializable]
    public sealed class GetAsnInfoRequest : EuiMessageBase
    {
        public string Asn { get; set; }

        public GetAsnInfoRequest(string asn)
        {
            Asn = asn;
        }
    }
}
