using Content.Shared.Damage;
using Content.Shared.Electrocution;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.Slippery;
using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory;

public partial class InventorySystem
{
    public void InitializeRelay()
    {
        SubscribeLocalEvent<InventoryComponent, DamageModifyEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, ElectrocutionAttemptEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, SlipAttemptEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, RefreshMovementSpeedModifiersEvent>(RelayInventoryEvent);
    }

    protected void RelayInventoryEvent<T>(EntityUid uid, InventoryComponent component, T args) where T : EntityEventArgs, IInventoryRelayEvent
    {
        var containerEnumerator = new ContainerSlotEnumerator(uid, component.TemplateId, _prototypeManager, this, args.TargetSlots);
        while(containerEnumerator.MoveNext(out var container))
        {
            if(!container.ContainedEntity.HasValue) continue;
            RaiseLocalEvent(container.ContainedEntity.Value, args, false);
        }
    }
}

/// <summary>
///     Events that should be relayed to inventory slots should implement this interface.
/// </summary>
public interface IInventoryRelayEvent
{
    /// <summary>
    ///     What inventory slots should this event be relayed to, if any?
    /// </summary>
    /// <remarks>
    ///     In general you may want to exclude <see cref="SlotFlags.POCKET"/>, given that those items are not truly
    ///     "equipped" by the user.
    /// </remarks>
    public SlotFlags TargetSlots { get; }
}
