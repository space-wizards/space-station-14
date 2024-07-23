using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Client.Inventory;
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
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client.Clothing;

public sealed class ClientClothingSystem : ClothingSystem
{
    public const string Jumpsuit = "jumpsuit";

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
        {Jumpsuit, "INNERCLOTHING"},
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
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingComponent, GetEquipmentVisualsEvent>(OnGetVisuals);

        SubscribeLocalEvent<InventoryComponent, VisualsChangedEvent>(OnVisualsChanged);
        SubscribeLocalEvent<SpriteComponent, DidUnequipEvent>(OnDidUnequip);
        SubscribeLocalEvent<InventoryComponent, AppearanceChangeEvent>(OnAppearanceUpdate);
    }

    private void OnAppearanceUpdate(EntityUid uid, InventoryComponent component, ref AppearanceChangeEvent args)
    {
        // May need to update jumpsuit stencils if the sex changed. Also required to properly set the stencil on init
        if (args.Sprite == null)
            return;

        if (_inventorySystem.TryGetSlotEntity(uid, Jumpsuit, out var suit, component)
            && TryComp(suit, out ClothingComponent? clothing))
        {
            SetGenderedMask(uid, args.Sprite, clothing);
            return;
        }

        // No clothing equipped -> make sure the layer is hidden, though this should already be handled by on-unequip.
        if (args.Sprite.LayerMapTryGet(HumanoidVisualLayers.StencilMask, out var layer))
        {
            DebugTools.Assert(!args.Sprite[layer].Visible);
            args.Sprite.LayerSetVisible(layer, false);
        }
    }

    private void OnGetVisuals(EntityUid uid, ClothingComponent item, GetEquipmentVisualsEvent args)
    {
        if (!TryComp(args.Equipee, out InventoryComponent? inventory))
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

            item.MappedLayer = key;
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
            rsi = _cache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / clothing.RsiPath).RSI;
        else if (TryComp(uid, out SpriteComponent? sprite))
            rsi = sprite.BaseRSI;

        if (rsi == null)
            return false;

        var correctedSlot = slot;
        TemporarySlotMap.TryGetValue(correctedSlot, out correctedSlot);



        var state = $"equipped-{correctedSlot}";

        if (clothing.EquippedPrefix != null)
            state = $"{clothing.EquippedPrefix}-equipped-{correctedSlot}";

        if (clothing.EquippedState != null)
            state = $"{clothing.EquippedState}";

        // species specific
        if (speciesId != null && rsi.TryGetState($"{state}-{speciesId}", out _))
            state = $"{state}-{speciesId}";
        else if (!rsi.TryGetState(state, out _))
            return false;

        var layer = new PrototypeLayerData();
        layer.RsiPath = rsi.Path.ToString();
        layer.State = state;
        layers = new() { layer };

        return true;
    }

    private void OnVisualsChanged(EntityUid uid, InventoryComponent component, VisualsChangedEvent args)
    {
        var item = GetEntity(args.Item);

        if (!TryComp(item, out ClothingComponent? clothing) || clothing.InSlot == null)
            return;

        RenderEquipment(uid, item, clothing.InSlot, component, null, clothing);
    }

    private void OnDidUnequip(EntityUid uid, SpriteComponent component, DidUnequipEvent args)
    {
        // Hide jumpsuit mask layer.
        if (args.Slot == Jumpsuit
            && TryComp(uid, out SpriteComponent? sprite)
            && sprite.LayerMapTryGet(HumanoidVisualLayers.StencilMask, out var maskLayer))
        {
                sprite.LayerSetVisible(maskLayer, false);
        }

        if (!TryComp(uid, out InventorySlotsComponent? inventorySlots))
            return;

        if (!inventorySlots.VisualLayerKeys.TryGetValue(args.Slot, out var revealedLayers))
            return;

        // Remove old layers. We could also just set them to invisible, but as items may add arbitrary layers, this
        // may eventually bloat the player with lots of invisible layers.
        foreach (var layer in revealedLayers)
        {
            component.RemoveLayer(layer);
        }
        revealedLayers.Clear();
    }

    public void InitClothing(EntityUid uid, InventoryComponent component)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        var enumerator = _inventorySystem.GetSlotEnumerator((uid, component));
        while (enumerator.NextItem(out var item, out var slot))
        {
            RenderEquipment(uid, item, slot.Name, component, sprite);
        }
    }

    protected override void OnGotEquipped(EntityUid uid, ClothingComponent component, GotEquippedEvent args)
    {
        base.OnGotEquipped(uid, component, args);

        RenderEquipment(args.Equipee, uid, args.Slot, clothingComponent: component);
    }

    private void RenderEquipment(EntityUid equipee, EntityUid equipment, string slot,
        InventoryComponent? inventory = null, SpriteComponent? sprite = null, ClothingComponent? clothingComponent = null,
        InventorySlotsComponent? inventorySlots = null)
    {
        if (!Resolve(equipee, ref inventory, ref sprite, ref inventorySlots) ||
           !Resolve(equipment, ref clothingComponent, false))
        {
            return;
        }

        if (slot == Jumpsuit)
            SetGenderedMask(equipee, sprite, clothingComponent);

        if (!_inventorySystem.TryGetSlot(equipee, slot, out var slotDef, inventory))
            return;

        // Remove old layers. We could also just set them to invisible, but as items may add arbitrary layers, this
        // may eventually bloat the player with lots of invisible layers.
        if (inventorySlots.VisualLayerKeys.TryGetValue(slot, out var revealedLayers))
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
            inventorySlots.VisualLayerKeys[slot] = revealedLayers;
        }

        var ev = new GetEquipmentVisualsEvent(equipee, slot);
        RaiseLocalEvent(equipment, ev);

        if (ev.Layers.Count == 0)
        {
            RaiseLocalEvent(equipment, new EquipmentVisualsUpdatedEvent(equipee, slot, revealedLayers), true);
            return;
        }

        // temporary, until layer draw depths get added. Basically: a layer with the key "slot" is being used as a
        // bookmark to determine where in the list of layers we should insert the clothing layers.
        bool slotLayerExists = sprite.LayerMapTryGet(slot, out var index);
        var displacementData = inventory.Displacements.GetValueOrDefault(slot);

        // add the new layers
        foreach (var (key, layerData) in ev.Layers)
        {
            if (!revealedLayers.Add(key))
            {
                Log.Warning($"Duplicate key for clothing visuals: {key}. Are multiple components attempting to modify the same layer? Equipment: {ToPrettyString(equipment)}");
                continue;
            }

            if (slotLayerExists)
            {
                index++;
                // note that every insertion requires reshuffling & remapping all the existing layers.
                sprite.AddBlankLayer(index);
                sprite.LayerMapSet(key, index);

                if (layerData.Color != null)
                    sprite.LayerSetColor(key, layerData.Color.Value);
                if (layerData.Scale != null)
                    sprite.LayerSetScale(key, layerData.Scale.Value);
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
            // Sprite "redactor" just a week away.
            if (slot == Jumpsuit)
                layerData.Shader ??= "StencilDraw";

            sprite.LayerSetData(index, layerData);
            layer.Offset += slotDef.Offset;

            if (displacementData != null)
            {
                if (displacementData.ShaderOverride != null)
                    sprite.LayerSetShader(index, displacementData.ShaderOverride);

                var displacementKey = $"{key}-displacement";
                if (!revealedLayers.Add(displacementKey))
                {
                    Log.Warning($"Duplicate key for clothing visuals DISPLACEMENT: {displacementKey}.");
                    continue;
                }

                var displacementLayer = _serialization.CreateCopy(displacementData.Layer, notNullableOverride: true);
                displacementLayer.CopyToShaderParameters!.LayerKey = key;

                // Add before main layer for this item.
                sprite.AddLayer(displacementLayer, index);
                sprite.LayerMapSet(displacementKey, index);

                revealedLayers.Add(displacementKey);
            }
        }

        RaiseLocalEvent(equipment, new EquipmentVisualsUpdatedEvent(equipee, slot, revealedLayers), true);
    }


    /// <summary>
    ///     Sets a sprite's gendered mask based on gender (obviously).
    /// </summary>
    /// <param name="sprite">Sprite to modify</param>
    /// <param name="humanoid">Humanoid, to get gender from</param>
    /// <param name="clothing">Clothing component, to get mask sprite type</param>
    private void SetGenderedMask(EntityUid uid, SpriteComponent sprite, ClothingComponent clothing)
    {
        if (!sprite.LayerMapTryGet(HumanoidVisualLayers.StencilMask, out var layer))
            return;

        ClothingMask mask;
        string prefix;

        switch (CompOrNull<HumanoidAppearanceComponent>(uid)?.Sex)
        {
            case Sex.Male:
                mask = clothing.MaleMask;
                prefix = "male_";
                break;
            case Sex.Female:
                mask = clothing.FemaleMask;
                prefix = "female_";
                break;
            default:
                mask = clothing.UnisexMask;
                prefix = "unisex_";
                break;
        }

        sprite.LayerSetState(layer, mask switch
        {
            ClothingMask.NoMask => $"{prefix}none",
            ClothingMask.UniformTop => $"{prefix}top",
            _ => $"{prefix}full",
        });
        sprite.LayerSetVisible(layer, true);
    }
}
