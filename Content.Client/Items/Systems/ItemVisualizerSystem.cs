using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Wieldable.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Reflection;

namespace Content.Client.Items.Systems;


public sealed class ItemVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly IReflectionManager _refMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ItemVisualizerComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<ItemVisualizerComponent, GetInhandVisualsEvent>(OnGetHeldVisuals,
            after: [typeof(ItemSystem)]);
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

        if (TryComp<WieldableComponent>(ent, out var wieldableComponent) && wieldableComponent.Wielded && ent.Comp.WieldedInhandVisuals.TryGetValue(args.Location, out var wieldedLayers))
            layers = wieldedLayers;

        var i = 0;
        var defaultKey = $"inhand-visualizer-{args.Location.ToString().ToLowerInvariant()}";
        foreach (var layer in layers)
        {
            if (layer.MapKeys == null)
            {
                args.Layers.Add((i == 0 ? defaultKey : $"{defaultKey}-{i}", layer));
                continue;
            }

            foreach (var key in layer.MapKeys)
            {
                if (!_refMan.TryParseEnumReference(key, out var value))
                    continue;

                if (!_appearance.TryGetData(ent.Owner, value, out var data, appearance))
                    continue;

                var layerdata = GetGenericLayerData(ent, layer, data, value, key);

                var finalLayer = layerdata ?? layer;

                var mapKey = key;
                if (string.IsNullOrEmpty(mapKey))
                {
                    mapKey = i == 0 ? defaultKey : $"{defaultKey}-{i}";
                    i++;
                }

                args.Layers.Add((mapKey, finalLayer));
            }
        }
    }

    private PrototypeLayerData? GetGenericLayerData(Entity<ItemVisualizerComponent> ent, PrototypeLayerData baseLayer, object data, Enum key, string mapKey)
    {
        var visuals = ent.Comp.Visuals;

        var appearanceValue = data.ToString();
        if (string.IsNullOrEmpty(appearanceValue))
            return null;

        if (!visuals.TryGetValue(key, out var layerDict))
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
        merged.MapKeys = overrideData.MapKeys ?? baseLayer.MapKeys;
        return merged;
    }
}
