using Content.Shared.Containers.ItemSlots;

namespace Content.Shared.CassetteTape.Components;

[RegisterComponent]
public sealed partial class CassetteTapeSlotComponent : Component
{
    /// <summary>
    /// The actual item-slot that contains the cassette tape. Allows all the interaction logic to be handled by <see cref="ItemSlotsSystem"/>.
    /// </summary>
    /// <remarks>
    /// Potentially, we could categorize tapes in terms of their physical size, tape length, and tape composition. Not yet, though.
    /// </remarks>
    [DataField("cassetteTapeSlotId", required: true)]
    public string CassetteTapeSlotId = string.Empty;
}
/// <summary>
/// Raised directed at an entity with a cassette tape slot when the tape inside is ejected/inserted.
/// </summary>
public sealed class CassetteTapeChangedEvent : EntityEventArgs
{
    public readonly bool Ejected;
    public readonly EntityUid Tape;

    public CassetteTapeChangedEvent(bool ejected, EntityUid tape)
    {
        Ejected = ejected;
        Tape = tape;
    }
}
