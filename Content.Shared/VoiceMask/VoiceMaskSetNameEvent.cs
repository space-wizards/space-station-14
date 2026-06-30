using Content.Shared.Actions;
using Content.Shared.Inventory;

namespace Content.Shared.VoiceMask;

public sealed partial class VoiceMaskSetNameEvent : InstantActionEvent
{
}

/// <summary>
/// Raised on an entity when their voice masks name is updated
/// </summary>
/// <param name="VoiceMask">VoiceMask component</param>
/// <param name="OldName">The old name</param>
/// <param name="NewName">The new name</param>
[ByRefEvent]
public readonly record struct VoiceMaskNameUpdatedEvent(Entity<VoiceMaskComponent> VoiceMask, string? OldName, string NewName) : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.WITHOUT_POCKET;
}
