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
        ToggleGear(ent, _proto.Index(ent.Comp.PrototypeID));
    }

    private void ToggleGear(Entity<EquipGearActionComponent> ent, StartingGearPrototype startingGear)
    {
        var (uid, comp) = ent;

        if (!ent.Comp.Equipped)
        {
            _spawning.EquipStartingGear(ent, startingGear, null);

            if (ent.Comp.PopupEquipSelf != string.Empty)
                _popup.PopupEntity(ent.Comp.PopupEquipSelf, ent, ent, ent.Comp.PopupType);

            if (ent.Comp.PopupEquipOthers != string.Empty)
                _popup.PopupEntity(ent.Comp.PopupEquipOthers, ent, Filter.PvsExcept(ent), true, ent.Comp.PopupType);
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
                        if (_inventory.TryGetSlotEntity(uid, slot.Name, out var slotItem))
                        {
                            if (TryComp<MetaDataComponent>(slotItem, out var slotItemMetaData))
                            {
                                if (TryPrototype(slotItem.Value, out var prototype, slotItemMetaData))
                                {
                                    if (prototype.ID == equipmentStr)
                                    {
                                        _inventory.TryUnequip(ent, slotItem.Value, slot.Name, true, force: true);
                                        QueueDel(slotItem);

                                        if (ent.Comp.PopupUnequipSelf != string.Empty)
                                            _popup.PopupEntity(ent.Comp.PopupUnequipSelf, ent, ent, ent.Comp.PopupType);

                                        if (ent.Comp.PopupUnequipOthers != string.Empty)
                                            _popup.PopupEntity(ent.Comp.PopupUnequipOthers, ent, Filter.PvsExcept(ent), true, ent.Comp.PopupType);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        ent.Comp.Equipped = !ent.Comp.Equipped;
    }
}