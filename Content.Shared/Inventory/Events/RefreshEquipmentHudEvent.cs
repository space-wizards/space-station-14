namespace Content.Shared.Inventory.Events;

public sealed class RefreshEquipmentHudEvent<T> : EntityEventArgs, IInventoryRelayEvent where T : IComponent
{
    public SlotFlags TargetSlots { get; init; }
    public bool Active = false;
    public List<T> Components = new();

    public RefreshEquipmentHudEvent(SlotFlags targetSlots)
    {
        TargetSlots = targetSlots;
    }
}
