namespace Content.Shared.Inventory.Events;

public sealed class GetActivatingComponentsEvent<T> : EntityEventArgs, IInventoryRelayEvent where T : IComponent
{
    public List<T> Components = new();

    public SlotFlags TargetSlots => SlotFlags.All;
}
