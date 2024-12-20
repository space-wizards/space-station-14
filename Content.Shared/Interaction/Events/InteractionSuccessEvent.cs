namespace Content.Shared.Interaction.Events;

/// <summary>
/// Raised on the target when successfully petting/hugging something.
/// </summary>
// TODO INTERACTION
// Rename this, or move it to another namespace to make it clearer that this is specific to "petting/hugging" (InteractionPopupSystem)
[ByRefEvent]
public readonly record struct InteractionSuccessEvent(EntityUid User);
