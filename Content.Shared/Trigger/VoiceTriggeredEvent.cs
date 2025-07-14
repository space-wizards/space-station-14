namespace Content.Shared.Trigger;

/// <summary>
/// Raised when a voice trigger is activated, containing the message that triggered it.
/// </summary>
/// <param name="Source"> The EntityUid of the entity sending the message</param>
/// <param name="Message"> The contents of the message</param>
[ByRefEvent]
public readonly record struct VoiceTriggeredEvent(EntityUid Source, string? Message);
