using System.Threading.Tasks;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Connection.Whitelist;

/// <summary>
/// Used by the <see cref="ConnectionManager"/> to determine if a player should be allowed to join the server.
/// Used in the whitelist.prototype_list CVar.
///
/// Whitelists are used to determine if a player is allowed to connect.
/// You define a PlayerConnectionWhitelist with a list of conditions.
/// Every condition has a type and a <see cref="ConditionAction"/> along with other parameters depending on the type.
/// Action must either be Allow, Deny or Next.
/// Allow means the player is instantly allowed to connect if the condition is met.
/// Deny means the player is instantly denied to connect if the condition is met.
/// Next means the next condition in the list is checked.
/// If the condition doesn't match, the next condition is checked.
/// </summary>
[Prototype("playerConnectionWhitelist")]
public sealed class PlayerConnectionWhitelistPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Minimum number of players required for this whitelist to be active.
    /// If there are less players than this, the whitelist will be ignored and the next one in the list will be used.
    /// </summary>
    [DataField]
    public int MinimumPlayers { get; } = 0;

    /// <summary>
    /// Maximum number of players allowed for this whitelist to be active.
    /// If there are more players than this, the whitelist will be ignored and the next one in the list will be used.
    /// </summary>
    [DataField]
    public int MaximumPlayers { get; } = int.MaxValue;

    [DataField]
    public WhitelistCondition[] Conditions { get; } = default!;
}
