namespace Content.Shared.Interaction.Events;

/// <summary>
/// Raised on the target when failing to pet/hug something.
/// </summary>
[ByRefEvent]
public readonly record struct InteractionFailureEvent(EntityUid User);
