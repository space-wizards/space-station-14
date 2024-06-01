namespace Content.Server.Chat.V2.Systems;

public sealed class ChatAttemptValidatedHandlerSystem : EntitySystem
{
    [Dependency]
    private ChatRepositorySystem _chatRepository = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ChatAttemptValidatedEvent>(ev => _chatRepository.Add(ev.Event));
    }
}
