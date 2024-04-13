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
using Robust.Shared.Audio.Systems;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Actions;

public sealed class EquipGearActionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly InventorySystem _inventory = default!;
    [Dependency] protected readonly SharedStationSpawningSystem _spawning = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EquipGearActionComponent, ActionPerformedEvent>(OnPerformed);
    }

    private void OnPerformed(Entity<EquipGearActionComponent> ent, ref ActionPerformedEvent args)
    {
        var startingGear = _proto.Index(ent.Comp.PrototypeID);
        ToggleGear(args.User, ent.Comp, startingGear);
    }

    private void ToggleGear(EntityUid ent, EquipGearActionComponent comp, StartingGearPrototype startingGear)
    {
        if (!comp.Equipped)
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
                            if (slotItem != null)
                                _inventory.TryUnequip(ent, slotItem.Value, slot.Name, true, force: true);
                        }
                    }
                }
            }

            if (!TryComp(ent, out HandsComponent? handsComponent))
            {
                var inhand = startingGear.Inhand;
                foreach (var prototype in inhand)
                {
                    if (_hands.TryGetEmptyHand(ent, out var emptyHand, handsComponent))
                    {
                        _popup.PopupEntity(Loc.GetString(comp.PopupNoFreehands), ent, ent);
                        return; // return if no available hands
                    }
                }
            }

            comp.EquippedItems = _spawning.EquipStartingGear(ent, startingGear, null);

            if (comp.PopupEquipSelf != string.Empty)
                _popup.PopupEntity(Loc.GetString(comp.PopupEquipSelf), ent, ent, comp.PopupType);

            if (comp.PopupEquipOthers != string.Empty)
                _popup.PopupEntity(Loc.GetString(comp.PopupEquipOthers), ent, Filter.PvsExcept(ent), true, comp.PopupType);
        }
        else
        {
            foreach (var uid in comp.EquippedItems)
                QueueDel(uid);

            _speedModifier.RefreshMovementSpeedModifiers(ent);

            if (comp.PopupUnequipSelf != string.Empty)
                _popup.PopupEntity(Loc.GetString(comp.PopupUnequipSelf), ent, ent, comp.PopupType);

            if (comp.PopupUnequipOthers != string.Empty)
                _popup.PopupEntity(Loc.GetString(comp.PopupUnequipOthers), ent, Filter.PvsExcept(ent), true, comp.PopupType);
        }

        comp.Equipped = !comp.Equipped;

        if (comp.ToggleSound != null)
            _audio.PlayPvs(comp.ToggleSound, ent);
    }
}