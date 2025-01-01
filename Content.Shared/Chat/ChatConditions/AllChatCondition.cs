using System.Linq;
using Content.Shared.Administration.Managers;
using Robust.Shared.Player;

namespace Content.Shared.Chat.ChatConditions;

/// <summary>
/// Return all consumers.
/// </summary>
[DataDefinition]
public sealed partial class AllChatCondition : ChatCondition
{
    public override HashSet<T> FilterConsumers<T>(HashSet<T> consumers, Dictionary<Enum, object> channelParameters) { return consumers; }
}
