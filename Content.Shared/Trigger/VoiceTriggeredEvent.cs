namespace Content.Shared.Trigger;

/// <summary>
/// Raised when a voice trigger is activated, containing the message that triggered it.
/// </summary>
/// <param name="Source"> The EntityUid of the entity sending the message</param>
/// <param name="Message"> The contents of the message</param>
/// <param name="MessageWithoutPhrase"> The message without the phrase that triggered it.</param>
[ByRefEvent]
public readonly record struct VoiceTriggeredEvent(EntityUid Source, string? Message, string MessageWithoutPhrase);
