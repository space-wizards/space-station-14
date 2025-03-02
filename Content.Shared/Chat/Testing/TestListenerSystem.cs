namespace Content.Shared.Chat.Testing;

public sealed class TestListenerSystem : ListenerEntitySystem<TestListenerComponent>
{

    public override void OnListenerMessageReceived(EntityUid uid, TestListenerComponent component, ListenerConsumeEvent args)
    {
        Logger.Debug("TestListenerSystem ran");
    }
}
