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
        if (!TryComp(target, out InventoryComponent? inventoryComponent))
            return false;

        if (!PrototypeManager.TryIndex<StartingGearPrototype>(gear, out var startingGear))
            return false;

        if (_invSystem.TryGetSlots(target, out var slots))
        {
            foreach (var slot in slots)
            {
                _invSystem.TryUnequip(target, slot.Name, true, true, false, inventoryComponent);
                var gearStr = ((IEquipmentLoadout)startingGear).GetGear(slot.Name);
                if (gearStr == string.Empty)
                    continue;

                var equipmentEntity = Spawn(gearStr, Comp<TransformComponent>(target).Coordinates);
                if (slot.Name == "id" &&
                    TryComp(equipmentEntity, out PdaComponent? pdaComponent) &&
                    TryComp<IdCardComponent>(pdaComponent.ContainedId, out var id))
                {
                    id.FullName = Comp<MetaDataComponent>(target).EntityName;
                }

                _invSystem.TryEquip(target, equipmentEntity, slot.Name, silent: true, force: true, inventory: inventoryComponent);
                if (unremovable)
                    EnsureComp<UnremoveableComponent>(equipmentEntity);

                onEquipped?.Invoke(target, equipmentEntity);
            }
        }

        if (TryComp(target, out HandsComponent? handsComponent))
        {
            var coords = Comp<TransformComponent>(target).Coordinates;
            foreach (var prototype in startingGear.Inhand)
            {
                var inhandEntity = Spawn(prototype, coords);
                _handSystem.TryPickup(target, inhandEntity, checkActionBlocker: false, handsComp: handsComponent);
            }
        }

        return true;
    }
}
