using System.Linq;
using Content.Client.Clothing;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Wieldable.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Reflection;

namespace Content.Client.Items.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class ItemVisualizerSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private ItemSystem _item = default!;
    [Dependency] private IReflectionManager _reflection = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ItemVisualizerComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<ItemVisualizerComponent, GetInhandVisualsEvent>(OnGetHeldVisuals, after: [typeof(ItemSystem)]);
        SubscribeLocalEvent<ItemVisualizerComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals, after: [typeof(ClientClothingSystem)]);
    }

    private void OnAppearanceChange(Entity<ItemVisualizerComponent> ent, ref AppearanceChangeEvent args)
    {
        _item.VisualsChanged(ent);
    }

    private void OnGetHeldVisuals(Entity<ItemVisualizerComponent> ent, ref GetInhandVisualsEvent args)
    {
        if (!TryComp(ent, out AppearanceComponent? appearance))
            return;

        if (!HasComp<ItemComponent>(ent))
            return;

        if (!ent.Comp.InhandVisuals.TryGetValue(args.Location, out var layers))
            return;

        if (TryComp<WieldableComponent>(ent, out var wieldableComponent)
            && wieldableComponent.Wielded
            && ent.Comp.WieldedInhandVisuals.TryGetValue(args.Location, out var wieldedLayers))
            layers = wieldedLayers;

        var defaultKey = $"inhand-visualizer-{args.Location.ToString().ToLowerInvariant()}";

        var newLayers = GetNewLayers((ent, ent.Comp, appearance), layers, defaultKey);

        args.Layers.AddRange(newLayers);
    }

    private void OnGetEquipmentVisuals(Entity<ItemVisualizerComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (!TryComp(ent.Owner, out AppearanceComponent? appearance))
            return;

        if (!TryComp(args.Equipee, out InventoryComponent? inventory))
            return;

        List<PrototypeLayerData>? layers = null;

        // attempt to get species specific data
        if (inventory.SpeciesId != null)
            ent.Comp.ClothingVisuals.TryGetValue($"{args.Slot}-{inventory.SpeciesId}", out layers);

        // No species specific data.  Try to default to generic data.
        if (layers == null && !ent.Comp.ClothingVisuals.TryGetValue(args.Slot, out layers))
            return;

        var defaultKey = $"equipment-visualizer-{args.Slot.ToLowerInvariant()}";

        var newLayers = GetNewLayers((ent, ent.Comp, appearance), layers, defaultKey);

        args.Layers.AddRange(newLayers);
    }

    private List<(string, PrototypeLayerData)> GetNewLayers(Entity<ItemVisualizerComponent, AppearanceComponent> ent, List<PrototypeLayerData> layers, string defaultKey)
    {
        List<(string, PrototypeLayerData)> addedLayers = new();

        var i = 0;
        foreach (var layer in layers)
        {
            if (layer.MapKeys == null)
            {
                addedLayers.Add((i == 0 ? defaultKey : $"{defaultKey}-{i}", layer));
                i++;
                continue;
            }

            var layerdata = GetGenericLayerData(ent, ent.Comp2, layer);
            var mapKey = i == 0 ? defaultKey : $"{defaultKey}-{i}";
            addedLayers.Add((mapKey, layerdata));
            i++;

        }
        return addedLayers;
    }

    /// <summary>
    /// Gets the layer data from generic visualizer, and applies it to our new layer
    /// </summary>
    private PrototypeLayerData GetGenericLayerData(Entity<ItemVisualizerComponent> ent, AppearanceComponent appearance, PrototypeLayerData baseLayer)
    {
        if (!TryComp<GenericVisualizerComponent>(ent, out var genericVisuals) || baseLayer.MapKeys == null)
            return baseLayer;

        var newLayer = baseLayer;

        foreach (var (appearanceKey, layerDict) in genericVisuals.Visuals)
        {
            if (!_appearance.TryGetData(ent.Owner, appearanceKey, out object? data, appearance))
                continue;

            var appearanceValue = data.ToString();

            if (string.IsNullOrEmpty(appearanceValue))
                continue;

            foreach (var (layerKeyRaw, layerDataDict) in layerDict)
            {
                if (!layerDataDict.TryGetValue(appearanceValue, out var layerData))
                    continue;

                object layerKey = _reflection.TryParseEnumReference(layerKeyRaw, out var @enum)
                    ? @enum
                    : layerKeyRaw;

                if (!baseLayer.MapKeys.Contains(layerKey))
                    continue;

                newLayer = MergeLayerData(newLayer, layerData);
            }
        }

        return newLayer;
    }

    private PrototypeLayerData MergeLayerData(PrototypeLayerData baseLayer, PrototypeLayerData overrideData)
    {
        var merged = new PrototypeLayerData();

        merged.Shader = overrideData.Shader ?? baseLayer.Shader;
        merged.TexturePath = overrideData.TexturePath ?? baseLayer.TexturePath;
        merged.RsiPath = overrideData.RsiPath ?? baseLayer.RsiPath;
        merged.State = overrideData.State ?? baseLayer.State;
        merged.Scale = overrideData.Scale ?? baseLayer.Scale;
        merged.Rotation = overrideData.Rotation ?? baseLayer.Rotation;
        merged.Offset = overrideData.Offset ?? baseLayer.Offset;
        merged.Visible = overrideData.Visible ?? baseLayer.Visible;
        merged.Color = overrideData.Color ?? baseLayer.Color;
        merged.MapKeys = null;
        return merged;
    }
}
