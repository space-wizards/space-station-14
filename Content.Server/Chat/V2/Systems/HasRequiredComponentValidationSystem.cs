using Content.Shared.Chat.V2.Components;
using Content.Shared.Chat.V2.Systems;

namespace Content.Server.Chat.V2.Systems;

public sealed class HasRequiredComponentValidationSystem : EntitySystem
{
    private const string MissingRequiredComponent = "chat-system-missing-required-component";

    private string _missingRequiredComponent = "";

    public override void Initialize()
    {
        base.Initialize();

        _missingRequiredComponent = Loc.GetString(MissingRequiredComponent);

        SubscribeLocalEvent<ChatSentEvent<VerbalChatSentEvent>>(OnValidationEvent<VerbalChatSentEvent, CanVerbalChatComponent>);
        SubscribeLocalEvent<ChatSentEvent<VisualChatSentEvent>>(OnValidationEvent<VisualChatSentEvent, CanVisualChatComponent>);
    }

    private void OnValidationEvent<T1, T2>(ChatSentEvent<T1> validationEvent, EntitySessionEventArgs args) where T1 : SendableChatEvent where T2 : Component
    {
        if (HasComp<T2>(GetEntity(validationEvent.Event.Sender)))
            validationEvent.Cancel(_missingRequiredComponent);
    }
}
