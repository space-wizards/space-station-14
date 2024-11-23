using System.Linq;
using Content.Shared.Administration.Managers;
using Robust.Shared.Player;

namespace Content.Shared.Chat.ChatConditions;

[DataDefinition]
public sealed partial class IsAdminSessionChatCondition : SessionChatCondition
{
    /// <summary>
    /// Checks if the consumers are admins.
    /// </summary>

    [Dependency] private readonly ISharedAdminManager _admin = default!;
    public override HashSet<ICommonSession> FilterConsumers(HashSet<ICommonSession> consumers)
    {
        IoCManager.InjectDependencies(this);

        var returnConsumers = new HashSet<ICommonSession>();

        foreach (var consumer in consumers)
        {
            if (_admin.IsAdmin(consumer, IncludeDeadmin))
                returnConsumers.Add(consumer);
        }

        return consumers.Where(x => _admin.IsAdmin(x, IncludeDeadmin)).ToHashSet();
    }

    [DataField]
    public bool IncludeDeadmin;
}
