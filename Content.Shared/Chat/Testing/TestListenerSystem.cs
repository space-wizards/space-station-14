using Robust.Shared.Utility;

namespace Content.Shared.Chat.Testing;

public sealed class TestListenerSystem : ListenerEntitySystem<TestListenerComponent>
{

    public override void OnListenerMessageReceived(EntityUid uid, TestListenerComponent component, FormattedMessage message)
    {
        Logger.Debug("TestListenerSystem ran");
    }
}
