using Content.Shared.Inventory;
namespace Content.Shared.AntiHypo;
public sealed class AntiHyposprayEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = ~SlotFlags.POCKET;
    public bool Inject;

    public AntiHyposprayEvent(bool inject)
    {
        Inject = inject;
    }
}
