using System.Collections.Immutable;
using System.Net;
using Content.Shared.Database;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server.Database;

public sealed class ServerUsernameRuleDef
{
    public int? Id {get; }
    public DateTimeOffset CreationTime { get; }
    public int? RoundId { get; }
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
        string expression,
        string message,
        NetUserId? restrictingAdmin,
        bool extendToBan = false,
        bool retired = false,
        NetUserId? retiringAdmin = null,
        DateTimeOffset? RetireTime = null)
    {
        if (string.IsNullOrWhiteSpace(expression)) {
            throw new ArgumentException("Expression must contain data");
        }

        Id = id;
        CreationTime = creationTime;
        roundId = roundId;
        Expression = expression;
        Message = message;
        restrictingAdmin = RestrictingAdmin;
        ExtendToBan = extendToBan;
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
