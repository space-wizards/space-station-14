using Content.Shared.Damage;
using Content.Shared.Electrocution;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.Slippery;
using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory;

public partial class InventorySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, DamageModifyEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, ElectrocutionAttemptEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, SlipAttemptEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, RefreshMovementSpeedModifiersEvent>(RelayInventoryEvent);
    }

    protected void RelayInventoryEvent<T>(EntityUid uid, InventoryComponent component, T args) where T : EntityEventArgs
    {
        var containerEnumerator = new ContainerSlotEnumerator(uid, component.TemplateId, _prototypeManager, this);
        while(containerEnumerator.MoveNext(out var container))
        {
            if(!container.ContainedEntity.HasValue) continue;
            RaiseLocalEvent(container.ContainedEntity.Value, args, false);
        }
    }
}
