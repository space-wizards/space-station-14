using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Connection.Whitelist;

[Prototype("whitelist")]
public sealed class WhitelistPrototype : IPrototype
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

public static class WhitelistExtensions
{
    public static (bool isWhitelisted, string? denyMessage) IsWhitelisted(this WhitelistPrototype prototype, NetUserData data, ISawmill sawmill)
    {
        foreach (var condition in prototype.Conditions)
        {
            if (condition.Condition(data))
                continue;

            sawmill.Debug($"User {data.UserName} failed whitelist condition {condition.GetType().Name}");
            return (false, condition.DenyMessage);
        }

        sawmill.Debug($"User {data.UserName} passed all whitelist conditions");
        return (true, null);
    }
}
