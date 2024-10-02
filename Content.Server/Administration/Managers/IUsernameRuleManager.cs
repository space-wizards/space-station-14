using System.Collections.Immutable;
using System.Net;
using System.Threading.Tasks;
using Content.Shared.Database;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Managers;

public interface IUsernameRuleManager
{
    public void Initialize();
    public void Restart();

    /// <summary>
    /// Restricts the specified username. Banning all connections witch match the expression
    /// </summary>
    /// <param name="expression">the username matching expression to be added</param>
    /// <param name="message">Reason for the restriction</param>
    /// <param name="restrictingAdmin">The person who created the restriction</param>
    /// <param name="extendToBan">Weather to prompt an extend the username ban to a full ban</param>
    public void CreateUsernameRule(string expression, string? message, NetUserId? restrictingAdmin, bool extendToBan = false);

    /// <summary>
    /// Removes a specified username restriction.
    /// </summary>
    /// <param name="restrictionId">The Id of the restriction being removed</param>
    /// <param name="removingAdmin">The person who removed the restriction</param>
    public Task RemoveUsernameRule(int restrictionId, NetUserId? removingAdmin);

    /// <summary>
    /// Gets the set of all active regex restrictions
    /// </summary>
    public Task<HashSet<string>?> GetUsernameRulesAsync();
}
