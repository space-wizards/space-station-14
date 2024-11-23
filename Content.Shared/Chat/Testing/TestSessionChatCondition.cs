using Robust.Shared.Player;

namespace Content.Shared.Chat.Testing;

public sealed partial class TestSessionChatCondition : SessionChatCondition
{
    public override HashSet<ICommonSession> FilterConsumers(HashSet<ICommonSession> consumers) { return consumers; }
}
