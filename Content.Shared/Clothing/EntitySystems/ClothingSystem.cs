using Content.Shared.Clothing.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Strip.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.EntitySystems;

public abstract class ClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _itemSys = default!;
    [Dependency] private readonly SharedContainerSystem _containerSys = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly InventorySystem _invSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ClothingComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<ClothingComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<ClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<ClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<ClothingComponent, ItemMaskToggledEvent>(OnMaskToggled);

        SubscribeLocalEvent<ClothingComponent, ClothingEquipDoAfterEvent>(OnEquipDoAfter);
        SubscribeLocalEvent<ClothingComponent, ClothingUnequipDoAfterEvent>(OnUnequipDoAfter);

        SubscribeLocalEvent<ClothingComponent, BeforeItemStrippedEvent>(OnItemStripped);
    }

    private void OnUseInHand(Entity<ClothingComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !ent.Comp.QuickEquip)
            return;

        var user = args.User;
        if (!TryComp(user, out InventoryComponent? inv) ||
            !TryComp(user, out HandsComponent? hands))
            return;

        QuickEquip(ent, (user, inv, hands));
        args.Handled = true;
        args.ApplyDelay = false;
    }

    private void QuickEquip(
        Entity<ClothingComponent> toEquipEnt,
        Entity<InventoryComponent, HandsComponent> userEnt)
    {
        foreach (var slotDef in userEnt.Comp1.Slots)
        {
            if (!_invSystem.CanEquip(userEnt, toEquipEnt, slotDef.Name, out _, slotDef, userEnt, toEquipEnt))
                continue;

            if (_invSystem.TryGetSlotEntity(userEnt, slotDef.Name, out var slotEntity, userEnt))
            {
                // Item in slot has to be quick equipable as well
                if (TryComp(slotEntity, out ClothingComponent? item) && !item.QuickEquip)
                    continue;

                if (!_invSystem.TryUnequip(userEnt, slotDef.Name, true, inventory: userEnt, checkDoafter: true))
                    continue;

                if (!_invSystem.TryEquip(userEnt, toEquipEnt, slotDef.Name, true, inventory: userEnt, clothing: toEquipEnt, checkDoafter: true))
                    continue;

                _handsSystem.PickupOrDrop(userEnt, slotEntity.Value, handsComp: userEnt);
            }
            else
            {
                if (!_invSystem.TryEquip(userEnt, toEquipEnt, slotDef.Name, true, inventory: userEnt, clothing: toEquipEnt, checkDoafter: true))
                    continue;
            }

            break;
        }
    }

    private void ToggleVisualLayers(EntityUid equipee, HashSet<HumanoidVisualLayers> layers, HashSet<HumanoidVisualLayers> appearanceLayers)
    {
        foreach (HumanoidVisualLayers layer in layers)
        {
            if (!appearanceLayers.Contains(layer))
                continue;

            InventorySystem.InventorySlotEnumerator enumerator = _invSystem.GetSlotEnumerator(equipee);

            bool shouldLayerShow = true;
            while (enumerator.NextItem(out EntityUid item, out SlotDefinition? slot))
            {
                if (TryComp(item, out HideLayerClothingComponent? comp))
                {
                    if (comp.Slots.Contains(layer))
                    {
                        if (TryComp(item, out ClothingComponent? clothing) && clothing.Slots == slot.SlotFlags)
                        {
                            //Checks for mask toggling. TODO: Make a generic system for this
                            if (comp.HideOnToggle && TryComp(item, out MaskComponent? mask))
                            {
                                if (clothing.EquippedPrefix != mask.EquippedPrefix)
                                {
                                    shouldLayerShow = false;
                                    break;
                                }
                            }
                            else
                            {
                                shouldLayerShow = false;
                                break;
                            }
                        }
                    }
                }
            }
            _humanoidSystem.SetLayerVisibility(equipee, layer, shouldLayerShow);
        }
    }

    protected virtual void OnGotEquipped(EntityUid uid, ClothingComponent component, GotEquippedEvent args)
    {
        component.InSlot = args.Slot;
        CheckEquipmentForLayerHide(args.Equipment, args.Equipee);

        if ((component.Slots & args.SlotFlags) != SlotFlags.NONE)
        {
            var gotEquippedEvent = new ClothingGotEquippedEvent(args.Equipee, component);
            RaiseLocalEvent(uid, ref gotEquippedEvent);

            var didEquippedEvent = new ClothingDidEquippedEvent((uid, component));
            RaiseLocalEvent(args.Equipee, ref didEquippedEvent);
        }
    }

    protected virtual void OnGotUnequipped(EntityUid uid, ClothingComponent component, GotUnequippedEvent args)
    {
        if ((component.Slots & args.SlotFlags) != SlotFlags.NONE)
        {
            var gotUnequippedEvent = new ClothingGotUnequippedEvent(args.Equipee, component);
            RaiseLocalEvent(uid, ref gotUnequippedEvent);

            var didUnequippedEvent = new ClothingDidUnequippedEvent((uid, component));
            RaiseLocalEvent(args.Equipee, ref didUnequippedEvent);
        }

        component.InSlot = null;
        CheckEquipmentForLayerHide(args.Equipment, args.Equipee);
    }

    private void OnGetState(EntityUid uid, ClothingComponent component, ref ComponentGetState args)
    {
        args.State = new ClothingComponentState(component.EquippedPrefix);
    }

    private void OnHandleState(EntityUid uid, ClothingComponent component, ref ComponentHandleState args)
    {
        if (args.Current is ClothingComponentState state)
        {
            SetEquippedPrefix(uid, state.EquippedPrefix, component);
            if (component.InSlot != null && _containerSys.TryGetContainingContainer((uid, null, null), out var container))
            {
                CheckEquipmentForLayerHide(uid, container.Owner);
            }
        }
    }

    private void OnMaskToggled(Entity<ClothingComponent> ent, ref ItemMaskToggledEvent args)
    {
        //TODO: sprites for 'pulled down' state. defaults to invisible due to no sprite with this prefix
        SetEquippedPrefix(ent, args.IsToggled ? args.equippedPrefix : null, ent);
        CheckEquipmentForLayerHide(ent.Owner, args.Wearer);
    }

    private void OnEquipDoAfter(Entity<ClothingComponent> ent, ref ClothingEquipDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;
        args.Handled = _invSystem.TryEquip(args.User, target, ent, args.Slot, clothing: ent.Comp, predicted: true, checkDoafter: false);
    }

    private void OnUnequipDoAfter(Entity<ClothingComponent> ent, ref ClothingUnequipDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;
        args.Handled = _invSystem.TryUnequip(args.User, target, args.Slot, clothing: ent.Comp, predicted: true, checkDoafter: false);
        if (args.Handled)
            _handsSystem.TryPickup(args.User, ent);
    }

    private void OnItemStripped(Entity<ClothingComponent> ent, ref BeforeItemStrippedEvent args)
    {
        args.Additive += ent.Comp.StripDelay;
    }

    private void CheckEquipmentForLayerHide(EntityUid equipment, EntityUid equipee)
    {
        if (TryComp(equipment, out HideLayerClothingComponent? clothesComp) && TryComp(equipee, out HumanoidAppearanceComponent? appearanceComp))
            ToggleVisualLayers(equipee, clothesComp.Slots, appearanceComp.HideLayersOnEquip);
    }

    #region Public API

    public void SetEquippedPrefix(EntityUid uid, string? prefix, ClothingComponent? clothing = null)
    {
        if (!Resolve(uid, ref clothing, false))
            return;

        if (clothing.EquippedPrefix == prefix)
            return;

        clothing.EquippedPrefix = prefix;
        _itemSys.VisualsChanged(uid);
        Dirty(uid, clothing);
    }

    public void SetSlots(EntityUid uid, SlotFlags slots, ClothingComponent? clothing = null)
    {
        if (!Resolve(uid, ref clothing))
            return;

        clothing.Slots = slots;
        Dirty(uid, clothing);
    }

    /// <summary>
    ///     Copy all clothing specific visuals from another item.
    /// </summary>
    public void CopyVisuals(EntityUid uid, ClothingComponent otherClothing, ClothingComponent? clothing = null)
    {
        if (!Resolve(uid, ref clothing))
            return;

        clothing.ClothingVisuals = otherClothing.ClothingVisuals;
        clothing.EquippedPrefix = otherClothing.EquippedPrefix;
        clothing.RsiPath = otherClothing.RsiPath;

        _itemSys.VisualsChanged(uid);
        Dirty(uid, clothing);
    }

    public void SetLayerColor(ClothingComponent clothing, string slot, string mapKey, Color? color)
    {
        foreach (var layer in clothing.ClothingVisuals[slot])
        {
            if (layer.MapKeys == null)
                return;

            if (!layer.MapKeys.Contains(mapKey))
                continue;

            layer.Color = color;
        }
    }
    public void SetLayerState(ClothingComponent clothing, string slot, string mapKey, string state)
    {
        foreach (var layer in clothing.ClothingVisuals[slot])
        {
            if (layer.MapKeys == null)
                return;

            if (!layer.MapKeys.Contains(mapKey))
                continue;

            layer.State = state;
        }
    }

    #endregion
}
