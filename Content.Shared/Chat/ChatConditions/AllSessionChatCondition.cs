using System.Linq;
using Content.Shared.Administration.Managers;
using Robust.Shared.Player;

namespace Content.Shared.Chat.ChatConditions;

/// <summary>
/// Return all sessions.
/// </summary>
[DataDefinition]
public sealed partial class AllSessionChatCondition : SessionChatCondition
{
    public override HashSet<ICommonSession> FilterConsumers(HashSet<ICommonSession> consumers, Dictionary<Enum, object> channelParameters) { return consumers; }
}
