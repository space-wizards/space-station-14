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

        SubscribeLocalEvent<ChatValidationEvent<AttemptVerbalChatEvent>>(OnValidationEvent<AttemptVerbalChatEvent, CanVerbalChatComponent>);
        SubscribeLocalEvent<ChatValidationEvent<AttemptVisualChatEvent>>(OnValidationEvent<AttemptVisualChatEvent, CanVisualChatComponent>);
    }

    private void OnValidationEvent<T1, T2>(ChatValidationEvent<T1> validationEvent, EntitySessionEventArgs args) where T1 : ChatAttemptEvent where T2 : Component
    {
        if (HasComp<T2>(GetEntity(validationEvent.Event.Sender)))
            validationEvent.Cancel(_missingRequiredComponent);
    }
}
