namespace Content.Shared.Chat;

public abstract class SharedChatSystem : EntitySystem
{
    protected ISawmill Sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        Sawmill = Logger.GetSawmill("chat");
    }
}
