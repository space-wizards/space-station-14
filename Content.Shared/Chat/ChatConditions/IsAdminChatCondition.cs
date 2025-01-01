using System.Linq;
using Content.Shared.Administration.Managers;
using Robust.Shared.Player;

namespace Content.Shared.Chat.ChatConditions;

/// <summary>
/// Checks if the consumers are admins.
/// </summary>
[DataDefinition]
public sealed partial class IsAdminChatCondition : ChatCondition
{
    public override Type? ConsumerType { get; set; } = typeof(ICommonSession);

    /// <summary>
    /// If true, deadmined sessions will be included.
    /// </summary>
    [DataField]
    public bool IncludeDeadmin;

    [Dependency] private readonly ISharedAdminManager _admin = default!;

    public override HashSet<T> FilterConsumers<T>(HashSet<T> consumers, Dictionary<Enum, object> channelParameters)
    {
        if (consumers is HashSet<ICommonSession> sessionConsumers)
        {
            IoCManager.InjectDependencies(this);

            var filteredConsumers = sessionConsumers.Where(x => _admin.IsAdmin(x, IncludeDeadmin)).ToHashSet();
            return filteredConsumers as HashSet<T> ?? new HashSet<T>();
        }

        return new HashSet<T>();
    }
}
