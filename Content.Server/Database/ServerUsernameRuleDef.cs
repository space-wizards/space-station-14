using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server.Database;

public sealed class ServerUsernameRuleDef
{
    public int? Id { get; }
    public DateTimeOffset CreationTime { get; }
    public int? RoundId { get; }
    public bool Regex { get; }
    public string Expression { get; }
    public string Message { get; }
    public NetUserId? RestrictingAdmin { get; }
    public bool ExtendToBan { get; }
    public bool Retired { get; }
    public NetUserId? RetiringAdmin { get; }
    public DateTimeOffset? RetireTime { get; }

    public ServerUsernameRuleDef(int? id,
        DateTimeOffset creationTime,
        int? roundId,
        bool regex,
        string expression,
        string message,
        NetUserId? restrictingAdmin,
        bool extendToBan = false,
        bool retired = false,
        NetUserId? retiringAdmin = null,
        DateTimeOffset? retireTime = null)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new ArgumentException("Expression must contain data");
        }

        Id = id;
        CreationTime = creationTime;
        RoundId = roundId;
        Regex = regex;
        Expression = expression;
        Message = message;
        RestrictingAdmin = restrictingAdmin;
        ExtendToBan = extendToBan;
        Retired = retired;
        RetiringAdmin = retiringAdmin;
        RetireTime = retireTime;
    }

    public string FormatUsernameViolationMessage(IConfigurationManager cfg, ILocalizationManager loc)
    {
        return $"""
                {loc.GetString("restrict-username-1")}
                {loc.GetString("restrict-username-2", ("reason", Message))}
                {loc.GetString("restrict-username-3")}
                """;
    }
}
