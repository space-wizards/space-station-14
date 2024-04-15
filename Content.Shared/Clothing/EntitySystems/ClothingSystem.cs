using Content.Shared.Clothing.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using System.Linq;

namespace Content.Shared.Clothing.EntitySystems;

public abstract class ClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _itemSys = default!;
    [Dependency] private readonly SharedContainerSystem _containerSys = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly InventorySystem _invSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    [ValidatePrototypeId<TagPrototype>]
    private const string HairTag = "HidesHair";

    [ValidatePrototypeId<TagPrototype>]
    private const string NoseTag = "HidesNose";

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

    private void ToggleVisualLayer(EntityUid equipee, HumanoidVisualLayers layer, string tag)
    {
        InventorySystem.InventorySlotEnumerator enumerator = _invSystem.GetSlotEnumerator(equipee);
        bool shouldLayerShow = true;
        while (enumerator.NextItem(out EntityUid item))
        {
            if (_tagSystem.HasTag(item, tag))
            {
                if (tag == NoseTag && TryComp(item, out MaskComponent? mask) && TryComp(item, out ClothingComponent? clothing)) //Special check needs to be made for NoseTag, due to masks being toggleable
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
        _humanoidSystem.SetLayerVisibility(equipee, layer, shouldLayerShow);
    }

    private void ToggleVisualLayers(EntityUid equipee, HashSet<HumanoidVisualLayers> layers)
    {
        InventorySystem.InventorySlotEnumerator enumerator = _invSystem.GetSlotEnumerator(equipee);

        foreach (HumanoidVisualLayers layer in layers)
        {
            bool shouldLayerShow = true;
            while (enumerator.NextItem(out EntityUid item))
            {
                if (TryComp(item, out HideLayerClothingComponent? comp))
                {
                    if (comp.HideOnToggle && TryComp(item, out MaskComponent? mask) && TryComp(item, out ClothingComponent? clothing)) //Special check needs to be made for NoseTag, due to masks being toggleable
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
            _humanoidSystem.SetLayerVisibility(equipee, layer, shouldLayerShow);
        }
    }

    protected virtual void OnGotEquipped(EntityUid uid, ClothingComponent component, GotEquippedEvent args)
    {
        component.InSlot = args.Slot;
        CheckEquipmentForLayerHide(args.Slot, args.Equipment, args.Equipee);
    }

    protected virtual void OnGotUnequipped(EntityUid uid, ClothingComponent component, GotUnequippedEvent args)
    {
        component.InSlot = null;
        CheckEquipmentForLayerHide(args.Slot, args.Equipment, args.Equipee);
    }

    private void OnGetState(EntityUid uid, ClothingComponent component, ref ComponentGetState args)
    {
        args.State = new ClothingComponentState(component.EquippedPrefix);
        if (component.InSlot != null && _containerSys.TryGetContainingContainer(uid, out var container))
        {
            CheckEquipmentForLayerHide(component.InSlot, uid, container.Owner);
        }
    }

    private void OnHandleState(EntityUid uid, ClothingComponent component, ref ComponentHandleState args)
    {
        if (args.Current is ClothingComponentState state)
        {
            SetEquippedPrefix(uid, state.EquippedPrefix, component);
            if (component.InSlot != null && _containerSys.TryGetContainingContainer(uid, out var container))
            {
                CheckEquipmentForLayerHide(component.InSlot, uid, container.Owner);
            }
        }
    }

    private void OnMaskToggled(Entity<ClothingComponent> ent, ref ItemMaskToggledEvent args)
    {
        //TODO: sprites for 'pulled down' state. defaults to invisible due to no sprite with this prefix
        SetEquippedPrefix(ent, args.IsToggled ? args.equippedPrefix : null, ent);
        if (TryComp(args.Wearer, out HumanoidAppearanceComponent? appearance) && appearance.HideNoseOnEquip)
            ToggleVisualLayer(args.Wearer, HumanoidVisualLayers.Snout, NoseTag);
    }

    private void OnEquipDoAfter(Entity<ClothingComponent> ent, ref ClothingEquipDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;
        args.Handled = _invSystem.TryEquip(args.User, target, ent, args.Slot, clothing: ent.Comp,  predicted: true, checkDoafter: false);
    }

    private void OnUnequipDoAfter(Entity<ClothingComponent> ent, ref ClothingUnequipDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;
        args.Handled = _invSystem.TryUnequip(args.User, target, args.Slot, clothing: ent.Comp, predicted: true, checkDoafter: false);
        if (args.Handled)
            _handsSystem.TryPickup(args.User, ent);
    }

    private void CheckEquipmentForLayerHide(string slot, EntityUid equipment, EntityUid equipee)
    {
        if (TryComp(equipment, out HideLayerClothingComponent? comp))
            ToggleVisualLayers(equipee, comp.Slots);

        /*if ((new string[] { "head" }).Contains(slot) && _tagSystem.HasTag(equipment, HairTag))
            ToggleVisualLayer(equipee, HumanoidVisualLayers.Hair, HairTag);
        if ((new string[] { "mask", "head" }).Contains(slot) && _tagSystem.HasTag(equipment, NoseTag))
            if (TryComp(equipee, out HumanoidAppearanceComponent? appearance) && appearance.HideNoseOnEquip)
                ToggleVisualLayer(equipee, HumanoidVisualLayers.Snout, NoseTag);*/
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
        clothing.FemaleMask = otherClothing.FemaleMask;

        _itemSys.VisualsChanged(uid);
        Dirty(uid, clothing);
    }

    #endregion
}
