using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Wieldable.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Reflection;

namespace Content.Client.Items.Systems;


public sealed class ItemVisualizerSystem : EntitySystem
{
    [Dependency] private readonly IResourceCache _resCache = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly IReflectionManager _refMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ItemVisualizerComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<ItemVisualizerComponent, GetInhandVisualsEvent>(OnGetHeldVisuals, after: [typeof(ItemSystem)]);
    }

    private void OnAppearanceChange(Entity<ItemVisualizerComponent> ent, ref AppearanceChangeEvent args)
    {
        _item.VisualsChanged(ent);
    }

    private void OnGetHeldVisuals(Entity<ItemVisualizerComponent> ent, ref GetInhandVisualsEvent args)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        if (!TryComp<ItemComponent>(ent, out var item))
            return;

        if (!ent.Comp.InhandVisuals.TryGetValue(args.Location, out var layers))
            return;

        TryComp<WieldableComponent>(ent, out var wieldable);

        var i = 0;
        foreach (var layer in layers)
        {
            if (layer.MapKeys == null)
            {
                var defaultKey = $"inhand-visualizer-{args.Location.ToString().ToLowerInvariant()}-{i}";
                var plainlayer = layer;
                plainlayer.RsiPath = item.RsiPath;
                Log.Debug("Layer added: " + layer.State);
                args.Layers.Add((defaultKey, plainlayer));
                i++;
                continue;
            }

            foreach (var key in layer.MapKeys)
            {
                if (!_refMan.TryParseEnumReference(key, out var value))
                    continue;

                if (!_appearance.TryGetData(ent.Owner, value, out var data, appearance))
                    continue;

                var layerdata = GetGenericLayerData(ent, data, value);

                if (layerdata != null)
                {
                    var newlayer = layerdata;
                    newlayer.State = layer.State;
                    newlayer.RsiPath = null;
                    if (wieldable != null && wieldable.Wielded)
                    {
                        newlayer.State = $"{wieldable.WieldedInhandPrefix}-{newlayer.State}";
                    }
                    args.Layers.Add((key, newlayer));
                    i++;
                }
            }
        }
    }

    private PrototypeLayerData? GetGenericLayerData(Entity<ItemVisualizerComponent> ent, object data, Enum key)
    {
        if (!TryComp<GenericVisualizerComponent>(ent, out var genericVisualizerComponent))
            return null;

        foreach (var (appearanceKey, layerDict) in genericVisualizerComponent.Visuals)
        {
            if (!Equals(appearanceKey, key))
                continue;

            var appearanceValue = data.ToString();
            if (string.IsNullOrEmpty(appearanceValue))
                continue;

            foreach (var (_, layerDataDict) in layerDict)
            {
                if (!layerDataDict.TryGetValue(appearanceValue, out var layerData))
                    continue;
                return layerData;
            }
        }
        return null;
    }

}
