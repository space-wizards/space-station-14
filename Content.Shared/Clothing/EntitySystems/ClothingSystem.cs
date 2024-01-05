using Content.Shared.Clothing.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Tag;
using Content.Shared.Timing;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.EntitySystems;

public abstract class ClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _itemSys = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly InventorySystem _invSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    [ValidatePrototypeId<TagPrototype>]
    private const string HairTag = "HidesHair";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ClothingComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<ClothingComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<ClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<ClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<ClothingComponent, ItemMaskToggledEvent>(OnMaskToggled);
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

                if (!_invSystem.TryUnequip(userEnt, slotDef.Name, true, inventory: userEnt, clothing: toEquipEnt))
                    continue;

                if (!_invSystem.TryEquip(userEnt, toEquipEnt, slotDef.Name, true, inventory: userEnt, clothing: toEquipEnt))
                    continue;

                _handsSystem.PickupOrDrop(userEnt, slotEntity.Value, handsComp: userEnt);
            }
            else
            {
                if (!_invSystem.TryEquip(userEnt, toEquipEnt, slotDef.Name, true, inventory: userEnt, clothing: toEquipEnt))
                    continue;
            }

            break;
        }
    }

    protected virtual void OnGotEquipped(EntityUid uid, ClothingComponent component, GotEquippedEvent args)
    {
        component.InSlot = args.Slot;
        if (args.Slot == "head" && _tagSystem.HasTag(args.Equipment, HairTag))
            _humanoidSystem.SetLayerVisibility(args.Equipee, HumanoidVisualLayers.Hair, false);
    }

    protected virtual void OnGotUnequipped(EntityUid uid, ClothingComponent component, GotUnequippedEvent args)
    {
        component.InSlot = null;
        if (args.Slot == "head" && _tagSystem.HasTag(args.Equipment, HairTag))
            _humanoidSystem.SetLayerVisibility(args.Equipee, HumanoidVisualLayers.Hair, true);
    }

    private void OnGetState(EntityUid uid, ClothingComponent component, ref ComponentGetState args)
    {
        args.State = new ClothingComponentState(component.EquippedPrefix);
    }

    private void OnHandleState(EntityUid uid, ClothingComponent component, ref ComponentHandleState args)
    {
        if (args.Current is ClothingComponentState state)
            SetEquippedPrefix(uid, state.EquippedPrefix, component);
    }

    private void OnMaskToggled(Entity<ClothingComponent> ent, ref ItemMaskToggledEvent args)
    {
        //TODO: sprites for 'pulled down' state. defaults to invisible due to no sprite with this prefix
        SetEquippedPrefix(ent, args.IsToggled ? "toggled" : null, ent);
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
