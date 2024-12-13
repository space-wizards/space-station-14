using System.Linq;
using Content.Shared.Administration.Managers;
using Robust.Shared.Player;

namespace Content.Shared.Chat.ChatConditions;

/// <summary>
/// Checks if the consumers are admins.
/// </summary>
[DataDefinition]
public sealed partial class IsAdminSessionChatCondition : SessionChatCondition
{
    [Dependency] private readonly ISharedAdminManager _admin = default!;
    public override HashSet<ICommonSession> FilterConsumers(HashSet<ICommonSession> consumers, Dictionary<Enum, object>? channelParameters)
    {
        IoCManager.InjectDependencies(this);

        return consumers.Where(x => _admin.IsAdmin(x, IncludeDeadmin)).ToHashSet();
    }

    [DataField]
    public bool IncludeDeadmin;
}
