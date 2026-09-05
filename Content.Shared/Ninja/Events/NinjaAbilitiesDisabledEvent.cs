using Content.Shared.Inventory;

namespace Content.Shared.Ninja.Events;

/// <summary>
/// This event will deactivate all ninja abilities for 5 seconds.
/// </summary>
[ByRefEvent]
public record struct NinjaAbilitiesDisabledEvent : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
}
