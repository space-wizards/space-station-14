using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;


[Serializable, NetSerializable]
public sealed class BanUsernamePanelEuiState : EuiStateBase
{
    public bool HasBan { get; set; }
    public bool HasHost { get; set; }

    public BanUsernamePanelEuiState(bool hasBan, bool hasHost)
    {
        HasBan = hasBan;
        HasHost = hasHost;
    }
}

public static class BanUsernamePanelEuiMsg
{
    /// <summary>
    /// used from client to server to create a new username ban.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class CreateUsernameBanRequest : EuiMessageBase
    {
        public readonly string RegexRule;
        public readonly string? Reason;
        public readonly bool Ban;
        public readonly bool Regex;

        public CreateUsernameBanRequest(string regexRule, string? reason, bool ban, bool regex)
        {
            RegexRule = regexRule;
            Reason = reason;
            Ban = ban;
            Regex = regex;
        }
    }

    /// <summary>
    /// used from client to server to request the full username ban info for a particular id.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class GetRuleInfoRequest : EuiMessageBase
    {
        public readonly int RuleId;

        public GetRuleInfoRequest(int ruleId)
        {
            RuleId = ruleId;
        }
    }

    /// <summary>
    /// used to send from server to client the full username ban info.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class FullUsernameRuleInfoReply : EuiMessageBase
    {
        public readonly DateTime CreationTime;
        public readonly int Id;
        public readonly bool Regex;
        public readonly bool ExtendToBan;
        public readonly bool Retired;
        public readonly int? RoundId;
        public readonly Guid? RestrictingAdmin;
        public readonly Guid? RetiringAdmin;
        public readonly DateTime? RetireTime;
        public readonly string Expression;
        public readonly string Message;

        public FullUsernameRuleInfoReply(
            DateTime creationTime,
            int id,
            bool regex,
            bool extendToBan,
            bool retired,
            int? roundId,
            Guid? restrictingAdmin,
            Guid? retiringAdmin,
            DateTime? retireTime,
            string expression,
            string message
        )
        {
            CreationTime = creationTime;
            Id = id;
            Regex = regex;
            ExtendToBan = extendToBan;
            Retired = retired;
            RoundId = roundId;
            RestrictingAdmin = restrictingAdmin;
            RetiringAdmin = retiringAdmin;
            RetireTime = retireTime;
            Expression = expression;
            Message = message;
        }
    }

    /// <summary>
    /// used form client to server to request the current list of all active username bans
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class UsernameRuleRefreshRequest : EuiMessageBase
    {
        // public void UsernameRuleRefreshRequest() { }
    }

    /// <summary>
    /// used form server to client to send updates about new or deleted username rules.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class UsernameRuleUpdate : EuiMessageBase
    {
        public readonly int Id;
        public readonly string Expression;
        public readonly bool Add;
        public readonly bool Regex;
        public readonly bool ExtendToBan;
        public readonly bool Silent;

        public UsernameRuleUpdate(
            string expression,
            int id,
            bool add,
            bool regex,
            bool extendToBan,
            bool silent = false
        )
        {
            Id = id;
            Add = add;
            Regex = regex;
            ExtendToBan = extendToBan;
            Expression = expression;
            Silent = silent;
        }
    }
}

public readonly record struct UsernameCacheLineUpdate(string Expression, int Id, bool ExtendToBan, bool Regex, bool Add);
public readonly record struct UsernameCacheLine(string Expression, int Id, bool ExtendToBan, bool Regex);
