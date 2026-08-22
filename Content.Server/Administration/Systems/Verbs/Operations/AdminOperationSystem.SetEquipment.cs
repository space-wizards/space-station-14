using Content.Server.Administration.Verbs.Operations;
using Content.Shared.Clothing.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnSetEquipment(Entity<InventoryComponent> entity, ref AdminOperationEvent<SetEquipmentOperation> args)
    {
        var operation = args.Operation;

        if (operation.StartingGear is { } startingGear)
            ApplyStartingGear(entity, startingGear, operation.Unremoveable);

        if (operation.ClearOtherSlots &&
            operation.StartingGear == null &&
            _inventory.TryGetSlots(entity, out var slots))
        {
            foreach (var slot in slots)
            {
                _inventory.TryUnequip(entity, slot.Name, true, true, inventory: entity.Comp);
            }
        }

        ApplyExplicitEquipment(entity, operation);
    }

    private void ApplyStartingGear(
        Entity<InventoryComponent> entity,
        ProtoId<StartingGearPrototype> startingGear,
        bool unremoveable)
    {
        _outfit.SetOutfit(entity, startingGear, (_, equipment) =>
        {
            if (unremoveable && HasComp<ClothingComponent>(equipment))
                EnsureComp<UnremoveableComponent>(equipment);
        });
    }

    private void ApplyExplicitEquipment(
        Entity<InventoryComponent> entity,
        SetEquipmentOperation operation)
    {
        foreach (var (slot, prototype) in operation.Equipment)
        {
            if (!operation.ClearOtherSlots || operation.StartingGear != null)
                _inventory.TryUnequip(entity, slot, true, true, inventory: entity.Comp);

            var equipment = Spawn(prototype, Transform(entity).Coordinates);
            if (!_inventory.TryEquip(entity, equipment, slot, true, true, inventory: entity.Comp))
            {
                QueueDel(equipment);
                continue;
            }

            if (operation.Unremoveable && HasComp<ClothingComponent>(equipment))
                EnsureComp<UnremoveableComponent>(equipment);
        }
    }
}
