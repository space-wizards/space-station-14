using Content.Shared.Inventory;

namespace Content.Server.NPC.Queries.Queries;

public sealed class ClothingSlotFilter : UtilityQueryFilter
{
    [DataField("slotFlags", required: true)]
    public SlotFlags SlotFlags = SlotFlags.NONE;
}
