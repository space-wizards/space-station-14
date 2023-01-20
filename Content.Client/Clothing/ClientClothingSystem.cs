using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Inventory;
using Content.Client.Humanoid;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using static Robust.Client.GameObjects.SpriteComponent;
using static Robust.Shared.GameObjects.SharedSpriteComponent;

namespace Content.Client.Clothing;

public sealed class ClientClothingSystem : ClothingSystem
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
        {"suitstorage", "SUITSTORAGE"},
    };

    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingComponent, GetEquipmentVisualsEvent>(OnGetVisuals);

        SubscribeLocalEvent<ClientInventoryComponent, VisualsChangedEvent>(OnVisualsChanged);
        SubscribeLocalEvent<SpriteComponent, DidUnequipEvent>(OnDidUnequip);
        SubscribeLocalEvent<ClientInventoryComponent, AppearanceChangeEvent>(OnAppearanceUpdate);
    }

    private void OnAppearanceUpdate(EntityUid uid, ClientInventoryComponent component, ref AppearanceChangeEvent args)
    {
        // May need to update jumpsuit stencils if the sex changed. Also required to properly set the stencil on init
        // when sex is first loaded from the profile.
        if (!TryComp(uid, out SpriteComponent? sprite) || !sprite.LayerMapTryGet(HumanoidVisualLayers.StencilMask, out var layer))
            return;

        if (!args.AppearanceData.TryGetValue(HumanoidVisualizerKey.Key, out object? obj)
            || obj is not HumanoidVisualizerData data
            || data.Sex != Sex.Female
            || !_inventorySystem.TryGetSlotEntity(uid, "jumpsuit", out var suit, component)
            || !TryComp(suit, out ClothingComponent? clothing))
        {
            sprite.LayerSetVisible(layer, false);
            return;
        }

        sprite.LayerSetState(layer, clothing.FemaleMask switch
        {
            FemaleClothingMask.NoMask => "female_none",
            FemaleClothingMask.UniformTop => "female_top",
            _ => "female_full",
        });
        sprite.LayerSetVisible(layer, true);
    }

    private void OnGetVisuals(EntityUid uid, ClothingComponent item, GetEquipmentVisualsEvent args)
    {
        if (!TryComp(args.Equipee, out ClientInventoryComponent? inventory))
            return;

        List<PrototypeLayerData>? layers = null;

        // first attempt to get species specific data.
        if (inventory.SpeciesId != null)
            item.ClothingVisuals.TryGetValue($"{args.Slot}-{inventory.SpeciesId}", out layers);

        // if that returned nothing, attempt to find generic data
        if (layers == null && !item.ClothingVisuals.TryGetValue(args.Slot, out layers))
        {
            // No generic data either. Attempt to generate defaults from the item's RSI & item-prefixes
            if (!TryGetDefaultVisuals(uid, item, args.Slot, inventory.SpeciesId, out layers))
                return;
        }

        // add each layer to the visuals
        var i = 0;
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                // using the $"{args.Slot}" layer key as the "bookmark" for layer ordering until layer draw depths get added
                key = $"{args.Slot}-{i}";
                i++;
            }

            args.Layers.Add((key, layer));
        }
    }

    /// <summary>
    ///     If no explicit clothing visuals were specified, this attempts to populate with default values.
    /// </summary>
    /// <remarks>
    ///     Useful for lazily adding clothing sprites without modifying yaml. And for backwards compatibility.
    /// </remarks>
    private bool TryGetDefaultVisuals(EntityUid uid, ClothingComponent clothing, string slot, string? speciesId,
        [NotNullWhen(true)] out List<PrototypeLayerData>? layers)
    {
        layers = null;

        RSI? rsi = null;

        if (clothing.RsiPath != null)
            rsi = _cache.GetResource<RSIResource>(TextureRoot / clothing.RsiPath).RSI;
        else if (TryComp(uid, out SpriteComponent? sprite))
            rsi = sprite.BaseRSI;

        if (rsi == null || rsi.Path == null)
            return false;

        var correctedSlot = slot;
        TemporarySlotMap.TryGetValue(correctedSlot, out correctedSlot);

        var state = (clothing.EquippedPrefix == null)
            ? $"equipped-{correctedSlot}"
            : $"{clothing.EquippedPrefix}-equipped-{correctedSlot}";

        // species specific
        if (speciesId != null && rsi.TryGetState($"{state}-{speciesId}", out _))
        {
            state = $"{state}-{speciesId}";
        }
        else if (!rsi.TryGetState(state, out _))
        {
            return false;
        }

        var layer = new PrototypeLayerData();
        layer.RsiPath = rsi.Path.ToString();
        layer.State = state;
        layers = new() { layer };

        return true;
    }

    private void OnVisualsChanged(EntityUid uid, ClientInventoryComponent component, VisualsChangedEvent args)
    {
        if (!TryComp(args.Item, out ClothingComponent? clothing) || clothing.InSlot == null)
            return;

        RenderEquipment(uid, args.Item, clothing.InSlot, component, null, clothing);
    }

    private void OnDidUnequip(EntityUid uid, SpriteComponent component, DidUnequipEvent args)
    {
        if (!TryComp(uid, out ClientInventoryComponent? inventory) || !TryComp(uid, out SpriteComponent? sprite))
            return;

        if (!inventory.VisualLayerKeys.TryGetValue(args.Slot, out var revealedLayers))
            return;

        // Remove old layers. We could also just set them to invisible, but as items may add arbitrary layers, this
        // may eventually bloat the player with lots of invisible layers.
        foreach (var layer in revealedLayers)
        {
            sprite.RemoveLayer(layer);
        }
        revealedLayers.Clear();
    }

    public void InitClothing(EntityUid uid, ClientInventoryComponent? component = null, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite, ref component) || !_inventorySystem.TryGetSlots(uid, out var slots, component))
            return;

        foreach (var slot in slots)
        {
            if (!_inventorySystem.TryGetSlotContainer(uid, slot.Name, out var containerSlot, out _, component) ||
                !containerSlot.ContainedEntity.HasValue) continue;

            RenderEquipment(uid, containerSlot.ContainedEntity.Value, slot.Name, component, sprite);
        }
    }

    protected override void OnGotEquipped(EntityUid uid, ClothingComponent component, GotEquippedEvent args)
    {
        base.OnGotEquipped(uid, component, args);

        RenderEquipment(args.Equipee, uid, args.Slot, clothingComponent: component);
    }

    private void RenderEquipment(EntityUid equipee, EntityUid equipment, string slot,
        ClientInventoryComponent? inventory = null, SpriteComponent? sprite = null, ClothingComponent? clothingComponent = null)
    {
        if(!Resolve(equipee, ref inventory, ref sprite) || !Resolve(equipment, ref clothingComponent, false))
            return;

        if (slot == "jumpsuit" && sprite.LayerMapTryGet(HumanoidVisualLayers.StencilMask, out var suitLayer))
        {
            if (_appearance.TryGetData<HumanoidVisualizerData>(equipee, HumanoidVisualizerKey.Key, out var data)
                && data.Sex == Sex.Female)
            {
                sprite.LayerSetState(suitLayer, clothingComponent.FemaleMask switch
                {
                    FemaleClothingMask.NoMask => "female_none",
                    FemaleClothingMask.UniformTop => "female_top",
                    _ => "female_full",
                });
                sprite.LayerSetVisible(suitLayer, true);
            }
            else
                sprite.LayerSetVisible(suitLayer, false);
        }

        if (!_inventorySystem.TryGetSlot(equipee, slot, out var slotDef, inventory))
            return;

        // Remove old layers. We could also just set them to invisible, but as items may add arbitrary layers, this
        // may eventually bloat the player with lots of invisible layers.
        if (inventory.VisualLayerKeys.TryGetValue(slot, out var revealedLayers))
        {
            foreach (var key in revealedLayers)
            {
                sprite.RemoveLayer(key);
            }
            revealedLayers.Clear();
        }
        else
        {
            revealedLayers = new();
            inventory.VisualLayerKeys[slot] = revealedLayers;
        }

        var ev = new GetEquipmentVisualsEvent(equipee, slot);
        RaiseLocalEvent(equipment, ev, false);

        if (ev.Layers.Count == 0)
        {
            RaiseLocalEvent(equipment, new EquipmentVisualsUpdatedEvent(equipee, slot, revealedLayers), true);
            return;
        }

        // temporary, until layer draw depths get added. Basically: a layer with the key "slot" is being used as a
        // bookmark to determine where in the list of layers we should insert the clothing layers.
        bool slotLayerExists = sprite.LayerMapTryGet(slot, out var index);

        // add the new layers
        foreach (var (key, layerData) in ev.Layers)
        {
            if (!revealedLayers.Add(key))
            {
                Logger.Warning($"Duplicate key for clothing visuals: {key}. Are multiple components attempting to modify the same layer? Equipment: {ToPrettyString(equipment)}");
                continue;
            }

            if (slotLayerExists)
            {
                index++;
                // note that every insertion requires reshuffling & remapping all the existing layers.
                sprite.AddBlankLayer(index);
                sprite.LayerMapSet(key, index);
            }
            else
                index = sprite.LayerMapReserveBlank(key);

            if (sprite[index] is not Layer layer)
                return;

            // In case no RSI is given, use the item's base RSI as a default. This cuts down on a lot of unnecessary yaml entries.
            if (layerData.RsiPath == null
                && layerData.TexturePath == null
                && layer.RSI == null
                && TryComp(equipment, out SpriteComponent? clothingSprite))
            {
                layer.SetRsi(clothingSprite.BaseRSI);
            }

            // Another "temporary" fix for clothing stencil masks.
            // Sprite layer redactor when
            if (slot == "jumpsuit")
                layerData.Shader ??= "StencilDraw";

            sprite.LayerSetData(index, layerData);
            layer.Offset += slotDef.Offset;
        }

        RaiseLocalEvent(equipment, new EquipmentVisualsUpdatedEvent(equipee, slot, revealedLayers), true);
    }
}
