namespace Content.Shared.Chat;

[ByRefEvent]
public record struct ClickMessageSenderEvent(EntityUid Sender, bool Handled = false);
