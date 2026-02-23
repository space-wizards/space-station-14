using Content.Shared.Access.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Systems;

public abstract class SharedOutfitSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] private readonly SharedHandsSystem _handSystem = default!;
    [Dependency] private readonly InventorySystem _invSystem = default!;

    public virtual bool SetOutfit(EntityUid target, string gear, Action<EntityUid, EntityUid>? onEquipped = null, bool unremovable = false, bool stripEmptySlots = true, bool respectEquippability = false)
    {
        if (!EntityManager.TryGetComponent(target, out InventoryComponent? inventoryComponent))
            return false;

        if (!PrototypeManager.TryIndex<StartingGearPrototype>(gear, out var startingGear))
            return false;

        if (_invSystem.TryGetSlots(target, out var slots))
        {
            foreach (var slot in slots)
            {
                var gearStr = ((IEquipmentLoadout)startingGear).GetGear(slot.Name);
                if (gearStr == string.Empty)
                {
                    if (stripEmptySlots == true)
                        _invSystem.TryUnequip(target, slot.Name, true, true, false, inventoryComponent);
                    continue;
                }

                var equipmentEntity = EntityManager.SpawnEntity(gearStr, EntityManager.GetComponent<TransformComponent>(target).Coordinates);
                if (slot.Name == "id" &&
                    EntityManager.TryGetComponent(equipmentEntity, out PdaComponent? pdaComponent) &&
                    EntityManager.TryGetComponent<IdCardComponent>(pdaComponent.ContainedId, out var id))
                {
                    id.FullName = EntityManager.GetComponent<MetaDataComponent>(target).EntityName;
                }

                if (respectEquippability && !_invSystem.CanEquip(target, equipmentEntity, slot.Name, out var reason, slotDefinition: slot, inventory: inventoryComponent, ignoreAccess: true))
                {
                    Del(equipmentEntity);
                    continue;
                }

                _invSystem.TryUnequip(target, slot.Name, true, true, false, inventoryComponent);

                _invSystem.TryEquip(target, equipmentEntity, slot.Name, silent: true, force: true, inventory: inventoryComponent);
                if (unremovable)
                    EnsureComp<UnremoveableComponent>(equipmentEntity);

                onEquipped?.Invoke(target, equipmentEntity);
            }
        }

        if (EntityManager.TryGetComponent(target, out HandsComponent? handsComponent))
        {
            var coords = EntityManager.GetComponent<TransformComponent>(target).Coordinates;
            foreach (var prototype in startingGear.Inhand)
            {
                var inhandEntity = EntityManager.SpawnEntity(prototype, coords);
                _handSystem.TryPickup(target, inhandEntity, checkActionBlocker: false, handsComp: handsComponent);
            }
        }

        return true;
    }
}
