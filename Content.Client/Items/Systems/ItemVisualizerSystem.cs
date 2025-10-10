using Content.Client.Clothing;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Wieldable.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Items.Systems;


public sealed class ItemVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

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

        var i = 0;
        var defaultKey = $"equipment-visualizer-{args.Slot.ToLowerInvariant()}";
        foreach (var layer in layers)
        {
            if (layer.MapKeys == null)
            {
                args.Layers.Add((i == 0 ? defaultKey : $"{defaultKey}-{i}", layer));
                i++;
                continue;
            }

            foreach (var key in layer.MapKeys)
            {
                var layerdata = GetGenericLayerData(ent, appearance, layer, key);
                var finalLayer = layerdata ?? layer;
                var layerKey = i == 0 ? defaultKey : $"{defaultKey}-{i}";
                args.Layers.Add((layerKey, finalLayer));
            }
        }

    }

    private void OnGetHeldVisuals(Entity<ItemVisualizerComponent> ent, ref GetInhandVisualsEvent args)
    {
        if (!TryComp(ent, out AppearanceComponent? appearance))
            return;

        if (!HasComp<ItemComponent>(ent))
            return;

        if (!ent.Comp.InhandVisuals.TryGetValue(args.Location, out var layers))
            return;

        if (TryComp<WieldableComponent>(ent, out var wieldableComponent) && wieldableComponent.Wielded && ent.Comp.WieldedInhandVisuals.TryGetValue(args.Location, out var wieldedLayers))
            layers = wieldedLayers;

        var i = 0;
        var defaultKey = $"inhand-visualizer-{args.Location.ToString().ToLowerInvariant()}";
        foreach (var layer in layers)
        {
            if (layer.MapKeys == null)
            {
                args.Layers.Add((i == 0 ? defaultKey : $"{defaultKey}-{i}", layer));
                i++;
                continue;
            }

            foreach (var key in layer.MapKeys)
            {
                var layerdata = GetGenericLayerData(ent, appearance, layer, key);
                var finalLayer = layerdata ?? layer;
                var mapKey = i == 0 ? defaultKey : $"{defaultKey}-{i}";
                args.Layers.Add((mapKey, finalLayer));
                i++;
            }
        }
    }

    private PrototypeLayerData? GetGenericLayerData(Entity<ItemVisualizerComponent> ent, AppearanceComponent appearance, PrototypeLayerData baseLayer, string mapKey)
    {
        if (!TryComp<GenericVisualizerComponent>(ent, out var genericVisuals))
            return null;

        foreach (var (appearanceKey, layerDict) in genericVisuals.Visuals)
        {
            if (!_appearance.TryGetData(ent.Owner, appearanceKey, out var data, appearance))
                continue;

            var appearanceValue = data.ToString();
            if (string.IsNullOrEmpty(appearanceValue))
                return null;

            if (!string.IsNullOrEmpty(mapKey) && layerDict.TryGetValue(mapKey, out var specificLayerDataDict))
            {
                if (specificLayerDataDict.TryGetValue(appearanceValue, out var overrideData))
                    return MergeLayerData(baseLayer, overrideData);
            }

            foreach (var layerDataDict in layerDict.Values)
            {
                if (layerDataDict.TryGetValue(appearanceValue, out var overrideData))
                    return MergeLayerData(baseLayer, overrideData);
            }
        }
        return null;
    }

    private static PrototypeLayerData MergeLayerData(PrototypeLayerData baseLayer, PrototypeLayerData overrideData)
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
        return merged;
    }
}
