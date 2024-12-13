using Robust.Shared.Player;

namespace Content.Shared.Chat.Testing;

[Serializable]
[DataDefinition]
public sealed partial class TestSessionChatCondition : SessionChatCondition
{
    public override HashSet<ICommonSession> FilterConsumers(HashSet<ICommonSession> consumers, Dictionary<Enum, object>? channelParameters) { return consumers; }
}
