using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Content.Shared.Clothing.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;

namespace Content.Server.Administration.Verbs.Operations;

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

/// <summary>
/// Equips optional starting gear, then overrides configured slots with explicit equipment.
/// </summary>
public sealed partial class SetEquipmentOperation : AdminOperationBase<SetEquipmentOperation>
{
    [DataField]
    public Dictionary<string, EntProtoId> Equipment { get; private set; } = new();

    /// <summary>
    /// Applied before <see cref="Equipment"/>; explicit entries replace gear in the same slots.
    /// </summary>
    [DataField]
    public ProtoId<StartingGearPrototype>? StartingGear { get; private set; }

    /// <summary>
    /// With no starting gear, clears every slot before equipping explicit entries.
    /// Starting gear always uses its normal outfit replacement behavior.
    /// </summary>
    [DataField]
    public bool ClearOtherSlots { get; private set; }

    /// <summary>
    /// Adds <c>UnremoveableComponent</c> to clothing equipped by this operation.
    /// </summary>
    [DataField]
    public bool Unremoveable { get; private set; }
}
