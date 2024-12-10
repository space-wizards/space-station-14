using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Network;


namespace Content.Server.Administration.Managers;

/// <summary>
/// Manages username rules; handles checks for banned usernames.
/// </summary>
public interface IUsernameRuleManager
{
    /// <summary>
    /// Start the manager by acquiring required data (username bans) and registering net messages
    /// </summary>
    void Initialize();

    /// <summary>
    /// refresh required data (username bans)
    /// </summary>
    void Restart();

    /// <summary>
    /// Invoked when new information is placed into or removed from the cache
    /// </summary>
    event Action<UsernameCacheLineUpdate>? UpdatedCache;

    /// <summary>
    /// All ban data presently in the cache
    /// </summary>
    IReadOnlyList<UsernameCacheLine> BanData { get; }

    /// <summary>
    /// Restricts the specified username. Banning all connections witch match the expression
    /// </summary>
    /// <param name="regex">weather the expression is regex if no this is a single user exact match ban</param>
    /// <param name="expression">the username matching expression to be added</param>
    /// <param name="message">Reason for the restriction</param>
    /// <param name="restrictingAdmin">The person who created the restriction</param>
    /// <param name="extendToBan">Weather to prompt an extend the username ban to a full ban</param>
    void CreateUsernameRule(bool regex, string expression, string message, NetUserId? restrictingAdmin, bool extendToBan = false);

    /// <summary>
    /// Removes a specified username restriction.
    /// </summary>
    /// <param name="restrictionId">The Id of the restriction being removed</param>
    /// <param name="removingAdmin">The person who removed the restriction</param>
    Task RemoveUsernameRule(int restrictionId, NetUserId? removingAdmin);

    /// <summary>
    /// acquire the ban def from the database
    /// </summary>
    /// <param name="banId">the target ban</param>
    /// <returns>the ban or null if ban does not exist</returns>
    Task<ServerUsernameRuleDef?> GetFullBanInfoAsync(int banId);

    /// <summary>
    /// Checks cached regex to see if username is presently banned.
    /// </summary>
    /// <param name="username">The username to be checked</param>
    /// <returns>Wether the username is banned, the username ban message, and if the user should be banned for that username</returns>
    Task<UsernameBanStatus> IsUsernameBannedAsync(string username);

    /// <summary>
    /// Adds a username to the username whitelist table
    /// </summary>
    /// <param name="username">The username to add</param>
    Task WhitelistAddUsernameAsync(string username);

    /// <summary>
    /// Removes a username to the username whitelist table
    /// </summary>
    /// <param name="username">The username to remove</param>
    /// <returns>If something was removed</returns>
    Task<bool> WhitelistRemoveUsernameAsync(string username);
}

public readonly record struct UsernameBanStatus(string Message, bool ExtendToBan, bool IsBanned);
