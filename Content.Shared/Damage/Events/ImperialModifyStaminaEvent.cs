using Content.Shared.Inventory;
namespace Content.Shared.Damage.Events;
public sealed class StaminaModifyEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = ~SlotFlags.POCKET;

    public readonly float OriginalDamage;
    public float Damage;
    public EntityUid? Origin;

    public StaminaModifyEvent(float damage, EntityUid? origin = null)
    {
        OriginalDamage = damage;
        Damage = damage;
        Origin = origin;
    }
}
