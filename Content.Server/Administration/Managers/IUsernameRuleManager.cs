using System.Threading.Tasks;
using Robust.Shared.Network;


namespace Content.Server.Administration.Managers;

public interface IUsernameRuleManager
{
    public void Initialize();
    public void Restart();

    /// <summary>
    /// Restricts the specified username. Banning all connections witch match the expression
    /// </summary>
    /// <param name="regex">weather the expression is regex if no this is a single user exact match ban</param>
    /// <param name="expression">the username matching expression to be added</param>
    /// <param name="message">Reason for the restriction</param>
    /// <param name="restrictingAdmin">The person who created the restriction</param>
    /// <param name="extendToBan">Weather to prompt an extend the username ban to a full ban</param>
    public void CreateUsernameRule(bool regex, string expression, string message, NetUserId? restrictingAdmin, bool extendToBan = false);

    /// <summary>
    /// Removes a specified username restriction.
    /// </summary>
    /// <param name="restrictionId">The Id of the restriction being removed</param>
    /// <param name="removingAdmin">The person who removed the restriction</param>
    public Task RemoveUsernameRule(int restrictionId, NetUserId? removingAdmin);

    /// <summary>
    /// Gets the set of all active regex restrictions
    /// </summary>
    public List<(int, string, string, bool)> GetUsernameRules();

    /// <summary>
    /// Checks cached regex to see if username is presently banned.
    /// </summary>
    /// <param name="username"></param>
    /// <returns>wether the username is banned, the username ban message, and if the user should be banned for that username</returns>
    public Task<(bool, string, bool)> IsUsernameBannedAsync(string username);

    /// <summary>
    /// Adds a username to the username whitelist table
    /// </summary>
    /// <param name="username"></param>
    public Task WhitelistAddUsernameAsync(string username);

    /// <summary>
    /// Removes a username to the username whitelist table
    /// </summary>
    /// <param name="username"></param>
    /// <returns>If something was removed</returns>
    public Task<bool> WhitelistRemoveUsernameAsync(string username);
}
