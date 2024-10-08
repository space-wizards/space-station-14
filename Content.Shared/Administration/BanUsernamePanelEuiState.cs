using System.Net;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;


[Serializable, NetSerializable]
public sealed class BanUsernamePanelEuiState : EuiStateBase
{
    public string PlayerName { get; set; }
    public bool HasBan { get; set; }
    public bool HasMassBan { get; set; }

    public BanUsernamePanelEuiState(string playerName, bool hasBan, bool hasMassBan)
    {
        PlayerName = playerName;
        HasBan = hasBan;
        HasMassBan = hasMassBan;
    }
}

public static class BanUsernamePanelEuiStateMsg
{
    [Serializable, NetSerializable]
    public sealed class CreateUsernameBanRequest : EuiMessageBase
    {
        public string RegexRule { get; set; }
        public string? Reason { get; set; }
        public bool Ban { get; set; }
        public bool Regex { get; set; }

        public CreateUsernameBanRequest(string regexRule, string? reason, bool ban, bool regex) {
            RegexRule = regexRule;
            Reason = reason;
            Ban = ban;
            Regex = regex;
        }
    }

    [Serializable, NetSerializable]
    public sealed class GetRuleInfoRequest : EuiMessageBase
    {
        public int RuleId { get; set; }

        public GetRuleInfoRequest(int ruleId) {
            RuleId = ruleId;
        }
    }
}
