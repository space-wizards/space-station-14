namespace Content.Shared.Chat;

[ByRefEvent]
public record struct ClickMessageSenderAttemptEvent(bool Handled = false);
