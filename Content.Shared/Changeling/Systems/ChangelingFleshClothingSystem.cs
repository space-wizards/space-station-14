using Content.Shared.Alert;
using Content.Shared.Changeling.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Systems;

public sealed class ChangelingFleshClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedChameleonClothingSystem _chameleonClothing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingFleshClothingAbilityComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChangelingFleshClothingAbilityComponent, ToggleFleshClothingEvent>(OnToggle);
        SubscribeLocalEvent<ChangelingFleshClothingAbilityComponent, BeforeChangelingTransformEvent>(OnBeforeChangelingTransform);
        SubscribeLocalEvent<ChangelingFleshClothingAbilityComponent, AfterChangelingTransformEvent>(OnAfterChangelingTransform);
    }

    private void OnMapInit(Entity<ChangelingFleshClothingAbilityComponent> ent, ref MapInitEvent args)
    {
        _alerts.ShowAlert(ent.Owner, ent.Comp.AlertId, 1);
    }

    private void OnToggle(Entity<ChangelingFleshClothingAbilityComponent> ent, ref ToggleFleshClothingEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.Enabled = !ent.Comp.Enabled;
        Dirty(ent);
        _alerts.ShowAlert(ent.Owner, ent.Comp.AlertId, (short)(ent.Comp.Enabled ? 1 : 0));

        args.Handled = true;
    }

    // If we transform into another species and loose an inventory item then it will get dropped as a result.
    // But we don't want fleeting clothing to do a sound and popup in that case,
    // so we have to delete any flesh clothing slots that would drop before transforming.
    private void OnBeforeChangelingTransform(Entity<ChangelingFleshClothingAbilityComponent> ent, ref BeforeChangelingTransformEvent args)
    {
        // We always remove slots that are no longer supported by the transformation, even if the component is disabled.
        RemoveRedundantFleshClothing(ent.Owner, args.StoredIdentity);
    }

    private void OnAfterChangelingTransform(Entity<ChangelingFleshClothingAbilityComponent> ent, ref AfterChangelingTransformEvent args)
    {
        if (ent.Comp.Enabled)
            SpawnAndTransformClothing(ent.Owner, args.StoredIdentity, ent.Comp.ClothingPrototypes);
    }

    /// <summary>
    /// Removes any flesh clothing items the target is wearing in slots the original does not have.
    /// </summary>
    public void RemoveRedundantFleshClothing(Entity<InventoryComponent?> target, Entity<InventoryComponent?> original)
    {
        // TODO: Not predicted yet because equipping is not predicted either and we don't want to be nude for a few frames.
        if (_net.IsClient)
            return;

        if (!Resolve(target, ref target.Comp))
            return;

        Resolve(original, ref original.Comp, false); // They might now have an inventory at all.

        var slots = _inventory.GetSlotEnumerator(target, SlotFlags.WITHOUT_POCKET);
        while (slots.NextItem(out var targetItem, out var slotDefinition))
        {
            if (!HasComp<ChangelingFleshClothingComponent>(targetItem))
                continue; // Do nothing for normal items.

            // If the original does not have that slot or it contains an invalid prototype then we remove any flesh clothing item in our corresponding slot.
            if (original.Comp == null
                || !_inventory.TryGetSlotEntity(original, slotDefinition.Name, out var originalItem, inventoryComponent: original.Comp)
                || MetaData(originalItem.Value).EntityPrototype?.ID == null)
            {
                _inventory.TryUnequip(target, slotDefinition.Name, silent: true, force: true, inventory: target.Comp);
                QueueDel(targetItem);
            }
        }
    }

    /// <summary>
    /// Spawns the given clothing items into empty inventory slots.
    /// If the item has <see cref ="ChameleonClothingComponent"/> and <see cref ="ChangelingFleshClothingComponent"/> then
    /// it will have its visuals changed to match the item another given player is wearing in the same slot.
    /// </summary>
    public void SpawnAndTransformClothing(Entity<InventoryComponent?> target, Entity<InventoryComponent?> original, Dictionary<string, EntProtoId> clothingPrototypes)
    {
        // TODO: Remove this guard when chameleon clothing is properly predicted.
        // Otherwise we will see the default item sprite for a moment after spawn until it's updated.
        if (_net.IsClient)
            return;

        if (!Resolve(target, ref target.Comp) || !Resolve(original, ref original.Comp, false)) // Don't log because the original might be outside PVS range since it's on another map.
            return;

        var slots = _inventory.GetSlotEnumerator(target, SlotFlags.WITHOUT_POCKET);
        var coords = Transform(target).Coordinates;
        while (slots.MoveNext(out var containerSlot, out var slotDefinition))
        {
            var targetItem = containerSlot.ContainedEntity;
            if (!_inventory.TryGetSlotEntity(original, slotDefinition.Name, out var originalItem, inventoryComponent: original.Comp)
                || MetaData(originalItem.Value).EntityPrototype?.ID is not { } chameleonProtoId)
                continue;

            // If our slot is empty then spawn a new chameleon clothing item inside.
            if (targetItem == null && clothingPrototypes.TryGetValue(slotDefinition.Name, out var fleshProtoId))
            {
                targetItem = SpawnAtPosition(fleshProtoId, coords);
                _inventory.TryEquip(target, targetItem.Value, slotDefinition.Name, silent: true, force: true, inventory: target.Comp);
            }

            // If the item in our slot is flesh clothing then set the chameleon prototype to mirror the target.
            if (HasComp<ChangelingFleshClothingComponent>(targetItem))
            {
                if (TryComp<ChameleonClothingComponent>(originalItem, out var originalChameleonComp))
                    _chameleonClothing.SetSelectedPrototype(targetItem.Value, originalChameleonComp.Default, validate: false); // If it is also a chameleon item then use whatever that is mimicing.
                else
                    _chameleonClothing.SetSelectedPrototype(targetItem.Value, chameleonProtoId, validate: false); // We don't validate because a lot of clothing is not chameleon whitelisted, like warden's beret.
            }
        }
    }
}

/// <summary>
/// Event raised to toggle the <see cref="ChangelingFleshClothingComponent"/>.
/// </summary>
public sealed partial class ToggleFleshClothingEvent : BaseAlertEvent;
