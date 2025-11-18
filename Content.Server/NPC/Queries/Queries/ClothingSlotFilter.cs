using Content.Shared.Inventory;

namespace Content.Server.NPC.Queries.Queries;

public sealed partial class ClothingSlotFilter : UtilityQueryFilter
{
    [DataField("slotFlags", required: true)]
    public SlotFlags SlotFlags = SlotFlags.NONE;
}
