using Content.Shared.Inventory;
using Content.Shared.Overlays;

namespace Content.Shared.NightVision;

[ByRefEvent]
public record struct RefreshNightVisionEvent(SlotFlags TargetSlots) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = TargetSlots;
    public List<NightVisionComponent> Components = new();
}
