using Content.Shared.TypingIndicator;

namespace Content.Server.TypingIndicator;

/// <summary>
/// This handles...
/// </summary>
public sealed class TypingIndicatorSystem : SharedTypingIndicatorSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<TypingChangedEvent>(OnTypingChanged);
    }

    private void OnTypingChanged(TypingChangedEvent msg, EntitySessionEventArgs args)
    {
        if (!TryComp<TypingIndicatorComponent>(args.SenderSession.AttachedEntity, out var typingIndicator))
            return;

        typingIndicator.Status = msg.Status;
        Dirty(typingIndicator);
    }
}
