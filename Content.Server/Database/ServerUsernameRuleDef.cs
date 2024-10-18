using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server.Database;

/// <summary>
/// This class is used as I/O for the database manager
/// </summary>
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
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);

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

    /// <summary>
    /// creates a formatted ban message besed off of the ban def
    /// </summary>
    /// <param name="cfg"></param>
    /// <param name="loc"></param>
    /// <returns>the formatted username ban message</returns>
    public string FormatUsernameViolationMessage(IConfigurationManager cfg, ILocalizationManager loc)
    {
        return $"""
                {loc.GetString("restrict-username-1")}
                {loc.GetString("restrict-username-2", ("reason", Message))}
                {loc.GetString("restrict-username-3")}
                """;
    }


    /// <summary>
    /// creates a formatted ban message based off of a provided message
    /// </summary>
    /// <param name="cfg"></param>
    /// <param name="loc"></param>
    /// <param name="message">the provided message</param>
    /// <returns>the formatted username ban message</returns>
    public static string FormatUsernameViolationMessage(IConfigurationManager cfg, ILocalizationManager loc, string message)
    {
        var change = cfg.GetCVar(CCVars.InfoLinksChangeUsername);
        return $"""
                {loc.GetString("restrict-username-1")}
                {loc.GetString("restrict-username-2", ("reason", message))}
                {loc.GetString("restrict-username-3")}
                {loc.GetString("restrict-username-4", ("link", change))}
                """;
    }
}
