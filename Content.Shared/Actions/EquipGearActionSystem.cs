using Content.Shared.Actions.Events;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Player;
using Content.Shared.Station;
using Content.Shared.IdentityManagement;

namespace Content.Shared.Actions;

public sealed class EquipGearActionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly InventorySystem _inventory = default!;
    [Dependency] protected readonly SharedStationSpawningSystem _spawning = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EquipGearActionComponent, ActionPerformedEvent>(OnPerform);
    }

    private void OnPerform(Entity<EquipGearActionComponent> ent, ref ActionPerformedEvent args)
    {
        ToggleGear(args.User, ent.Comp, _proto.Index(ent.Comp.PrototypeID));
    }

    private void ToggleGear(EntityUid ent, EquipGearActionComponent comp, StartingGearPrototype startingGear)
    {
        if (!comp.Equipped)
        {
            _spawning.EquipStartingGear(ent, startingGear, null);

            if (comp.PopupEquipSelf != string.Empty)
                _popup.PopupEntity(comp.PopupEquipSelf, ent, ent, comp.PopupType);

            if (comp.PopupEquipOthers != string.Empty)
                _popup.PopupEntity(comp.PopupEquipOthers, ent, Filter.PvsExcept(ent), true, comp.PopupType);
        }
        else
        {
            if (_inventory.TryGetSlots(ent, out var slotDefinitions))
            {
                foreach (var slot in slotDefinitions)
                {
                    var equipmentStr = startingGear.GetGear(slot.Name, null);
                    if (!string.IsNullOrEmpty(equipmentStr))
                    {
                        if (_inventory.TryGetSlotEntity(ent, slot.Name, out var slotItem))
                        {
                            if (TryComp<MetaDataComponent>(slotItem, out var slotItemMetaData))
                            {
                                if (TryPrototype(slotItem.Value, out var prototype, slotItemMetaData))
                                {
                                    if (prototype.ID == equipmentStr)
                                    {
                                        _inventory.TryUnequip(ent, slotItem.Value, slot.Name, true, force: true);
                                        QueueDel(slotItem);

                                        if (comp.PopupUnequipSelf != string.Empty)
                                            _popup.PopupEntity(comp.PopupUnequipSelf, ent, ent, comp.PopupType);

                                        if (comp.PopupUnequipOthers != string.Empty)
                                            _popup.PopupEntity(comp.PopupUnequipOthers, ent, Filter.PvsExcept(ent), true, comp.PopupType);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        comp.Equipped = !comp.Equipped;
    }
}