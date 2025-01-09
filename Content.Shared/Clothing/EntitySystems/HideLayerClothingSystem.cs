using Content.Shared.Clothing.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class HideLayerClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HideLayerClothingComponent, ClothingGotUnequippedEvent>(OnHideGotUnequipped);
        SubscribeLocalEvent<HideLayerClothingComponent, ClothingGotEquippedEvent>(OnHideGotEquipped);
        SubscribeLocalEvent<HideLayerClothingComponent, ItemMaskToggledEvent>(OnHideToggled);
    }

    private void OnHideToggled(Entity<HideLayerClothingComponent> ent, ref ItemMaskToggledEvent args)
    {
        SetLayerVisibility(ent!, args.Wearer, hideLayers: true);
    }

    private void OnHideGotEquipped(Entity<HideLayerClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        SetLayerVisibility(ent!, args.Wearer, hideLayers: true);
    }

    private void OnHideGotUnequipped(Entity<HideLayerClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        SetLayerVisibility(ent!, args.Wearer, hideLayers: false);
    }

    public void RefreshLayerVisibility(Entity<HideLayerClothingComponent?, ClothingComponent?> clothing)
    {
        // TODO Why does this exist?
        // Was it just in case something accidentally changes the result of IsEnabled() without triggering an update?

        if (!Resolve(clothing.Owner, ref clothing.Comp1, ref clothing.Comp2, false))
            return;

        if (clothing.Comp2.InSlot == null)
            return;

        if (!_container.TryGetContainingContainer((clothing.Owner, null, null), out var container))
            return;

        if (!TryComp(container.Owner, out HumanoidAppearanceComponent? human))
            return;

        SetLayerVisibility(clothing, (container.Owner, human), true);
    }

    private void SetLayerVisibility(
        Entity<HideLayerClothingComponent?, ClothingComponent?> clothing,
        Entity<HumanoidAppearanceComponent?> user,
        bool hideLayers)
    {
        if (!Resolve(clothing.Owner, ref clothing.Comp1, ref clothing.Comp2))
            return;

        if (!Resolve(user.Owner, ref user.Comp))
            return;

        hideLayers &= IsEnabled(clothing!);

        var hideable = user.Comp.HideLayersOnEquip;
        var inSlot = clothing.Comp2.InSlot?.Flag ?? SlotFlags.NONE;

        // This method should only be getting called while the clothing is equipped (though possibly currently in
        // the process of getting unequipped).
        DebugTools.AssertNotNull(clothing.Comp2.InSlot);
        DebugTools.AssertNotEqual(inSlot, SlotFlags.NONE);

        var dirty = false;

        // iterate the HideLayerClothingComponent's layers map and check that
        // the clothing is (or was)equipped in a matching slot.
        foreach (var (layer, validSlots) in clothing.Comp1.Layers)
        {
            if (!hideable.Contains(layer))
                continue;

            // Only update this layer if we are currently equipped to the relevant slot.
            if (validSlots.HasFlag(inSlot))
                _humanoid.SetLayerVisibility(user!, layer, !hideLayers, inSlot, ref dirty);
        }

        // Fallback for obsolete field: assume we want to hide **all** layers, as long as we are equipped to any
        // relevant clothing slot
#pragma warning disable CS0618 // Type or member is obsolete
        if (clothing.Comp1.Slots is { } slots && clothing.Comp2.Slots.HasFlag(inSlot))
#pragma warning restore CS0618 // Type or member is obsolete
        {
            foreach (var layer in slots)
            {
                if (hideable.Contains(layer))
                    _humanoid.SetLayerVisibility(user!, layer, !hideLayers, inSlot, ref dirty);
            }
        }

        if (dirty)
            Dirty(user!);
    }

    private bool IsEnabled(Entity<HideLayerClothingComponent, ClothingComponent> clothing)
    {
        // TODO Generalize this
        // I.e., make this and mask component use some generic toggleable.

        if (!clothing.Comp1.HideOnToggle)
            return true;

        if (!TryComp(clothing, out MaskComponent? mask))
            return true;

        return mask.IsToggled;
    }
}
