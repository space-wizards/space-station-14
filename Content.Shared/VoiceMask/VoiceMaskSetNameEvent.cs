using Content.Shared.Actions;

namespace Content.Shared.VoiceMask;

public sealed partial class VoiceMaskSetNameEvent : InstantActionEvent
{
}

/// <summary>
/// Raised on an entity when their voice masks name is updated
/// </summary>
/// <param name="VoiceMaskUid">Uid of the voice mask</param>
/// <param name="OldName">The old name</param>
/// <param name="NewName">The new name</param>
[ByRefEvent]
public readonly record struct VoiceMaskNameUpdatedEvent(EntityUid VoiceMaskUid, string? OldName, string NewName);
