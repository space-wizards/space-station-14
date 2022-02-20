using System.Collections.Generic;
using Content.Client.Inventory;
using Content.Shared.CharacterAppearance;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;

namespace Content.Client.Clothing;

public sealed class ClothingSystem : EntitySystem
{
    /// <summary>
    /// This is a shitty hotfix written by me (Paul) to save me from renaming all files.
    /// For some context, im currently refactoring inventory. Part of that is slots not being indexed by a massive enum anymore, but by strings.
    /// Problem here: Every rsi-state is using the old enum-names in their state. I already used the new inventoryslots ALOT. tldr: its this or another week of renaming files.
    /// </summary>
    private static readonly Dictionary<string, string> TemporarySlotMap = new()
    {
        {"head", "HELMET"},
        {"eyes", "EYES"},
        {"ears", "EARS"},
        {"mask", "MASK"},
        {"outerClothing", "OUTERCLOTHING"},
        {"jumpsuit", "INNERCLOTHING"},
        {"neck", "NECK"},
        {"back", "BACKPACK"},
        {"belt", "BELT"},
        {"gloves", "HAND"},
        {"shoes", "FEET"},
        {"id", "IDCARD"},
        {"pocket1", "POCKET1"},
        {"pocket2", "POCKET2"},
    };

    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private InventorySystem _inventorySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<ClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<ClientInventoryComponent, ItemPrefixChangeEvent>(OnPrefixChanged);
        SubscribeLocalEvent<SpriteComponent, DidUnequipEvent>(OnDidUnequip);
    }

    private void OnPrefixChanged(EntityUid uid, ClientInventoryComponent component, ItemPrefixChangeEvent args)
    {
        if (!TryComp(args.Item, out ClothingComponent? clothing) || clothing.InSlot == null)
            return;

        RenderEquipment(uid, args.Item, clothing.InSlot, component, null, clothing);
    }

    private void OnGotUnequipped(EntityUid uid, ClothingComponent component, GotUnequippedEvent args)
    {
        component.InSlot = null;
    }

    private void OnDidUnequip(EntityUid uid, SpriteComponent component, DidUnequipEvent args)
    {
        component.LayerSetVisible(args.Slot, false);
    }

    public void InitClothing(EntityUid uid, ClientInventoryComponent? component = null, SpriteComponent? sprite = null)
    {
        if (!_inventorySystem.TryGetSlots(uid, out var slots, component) || !Resolve(uid, ref sprite, ref component)) return;

        foreach (var slot in slots)
        {
            sprite.LayerMapReserveBlank(slot.Name);

            if (!_inventorySystem.TryGetSlotContainer(uid, slot.Name, out var containerSlot, out _, component) ||
                !containerSlot.ContainedEntity.HasValue) continue;

            RenderEquipment(uid, containerSlot.ContainedEntity.Value, slot.Name, component, sprite);
        }
    }

    private void OnGotEquipped(EntityUid uid, ClothingComponent component, GotEquippedEvent args)
    {
        component.InSlot = args.Slot;

        if (!TryComp<SpriteComponent>(args.Equipee, out var sprite) || !TryComp<ClientInventoryComponent>(args.Equipee, out var invComp))
        {
            return;
        }

        var data = GetEquippedStateInfo(args.Equipment, args.Slot, invComp.SpeciesId, component);
        if (data != null)
        {
            var (rsi, state) = data.Value;
            sprite.LayerSetVisible(args.Slot, true);
            sprite.LayerSetState(args.Slot, state, rsi);
            sprite.LayerSetAutoAnimated(args.Slot, true);

            if (args.Slot == "jumpsuit" && sprite.LayerMapTryGet(HumanoidVisualLayers.StencilMask, out _))
            {
                sprite.LayerSetState(HumanoidVisualLayers.StencilMask, component.FemaleMask switch
                {
                    FemaleClothingMask.NoMask => "female_none",
                    FemaleClothingMask.UniformTop => "female_top",
                    _ => "female_full",
                });
            }

            return;
        }


        sprite.LayerSetVisible(args.Slot, false);
    }

    private void RenderEquipment(EntityUid uid, EntityUid equipment, string slot,
        ClientInventoryComponent? inventoryComponent = null, SpriteComponent? sprite = null, ClothingComponent? clothingComponent = null)
    {
        if(!Resolve(uid, ref inventoryComponent, ref sprite))
            return;

        if (!Resolve(equipment, ref clothingComponent, false))
        {
            sprite.LayerSetVisible(slot, false);
            return;
        }

        var data = GetEquippedStateInfo(equipment, slot, inventoryComponent.SpeciesId, clothingComponent);
        if (data == null) return;
        var (rsi, state) = data.Value;
        sprite.LayerSetVisible(slot, true);
        sprite.LayerSetState(slot, state, rsi);
        sprite.LayerSetAutoAnimated(slot, true);

        if (slot == "jumpsuit" && sprite.LayerMapTryGet(HumanoidVisualLayers.StencilMask, out _))
        {
            sprite.LayerSetState(HumanoidVisualLayers.StencilMask, clothingComponent.FemaleMask switch
            {
                FemaleClothingMask.NoMask => "female_none",
                FemaleClothingMask.UniformTop => "female_top",
                _ => "female_full",
            });
        }
    }

    public (RSI rsi, RSI.StateId stateId)? GetEquippedStateInfo(EntityUid uid, string slot, string? speciesId=null, ClothingComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return null;

        if (component.RsiPath == null)
            return null;

        var rsi = _cache.GetResource<RSIResource>(SharedSpriteComponent.TextureRoot / component.RsiPath).RSI;
        var correctedSlot = slot;
        TemporarySlotMap.TryGetValue(correctedSlot, out correctedSlot);
        var stateId = component.EquippedPrefix != null ? $"{component.EquippedPrefix}-equipped-{correctedSlot}" : $"equipped-{correctedSlot}";
        if (speciesId != null)
        {
            var speciesState = $"{stateId}-{speciesId}";
            if (rsi.TryGetState(speciesState, out _))
            {
                return (rsi, speciesState);
            }
        }

        if (rsi.TryGetState(stateId, out _))
        {
            return (rsi, stateId);
        }

        return null;
    }
}
