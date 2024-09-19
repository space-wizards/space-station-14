namespace Content.Shared.Interaction.Events;

/// <summary>
/// Raised on the target when successfully petting/hugging something.
/// </summary>
[ByRefEvent]
public readonly record struct InteractionSuccessEvent(EntityUid User);
