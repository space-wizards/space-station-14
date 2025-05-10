using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.Storage.EntitySystems;

public abstract class SharedItemChangeLayerColorSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangeLayersColorComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ChangeLayersColorComponent, EntInsertedIntoContainerMessage>(OnEntInsert);
        SubscribeLocalEvent<ChangeLayersColorComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
    }

    private void OnEntInsert(EntityUid uid, ChangeLayersColorComponent comp, ref EntInsertedIntoContainerMessage args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        var ent = args.Entity;

        if (!TryComp<ItemLayersColorComponent>(ent, out var itemLayerColor))
            return;

        UpdateAppearance(uid, ent, itemLayerColor, comp, appearance);
    }

    private void OnEntRemoved(EntityUid uid, ChangeLayersColorComponent comp, ref EntRemovedFromContainerMessage args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        var ent = args.Entity;

        if (!TryComp<ItemLayersColorComponent>(ent, out var itemLayerColor))
            return;

        UpdateAppearance(uid, ent, itemLayerColor, comp, appearance);
    }

    private void UpdateAppearance(EntityUid parent, EntityUid item, ItemLayersColorComponent itemLayerColor, ChangeLayersColorComponent comp, AppearanceComponent appearance)
    {
        var dict = new Dictionary<string, Color>();
        if(!TryGetLayer(item, comp, out var layer))
            return;

        dict.Add(layer, itemLayerColor.Color);
        _appearance.SetData(parent, StorageMapVisuals.LayerChanged, new ColorLayerData(dict), appearance);
    }

    private void OnComponentInit(EntityUid uid, ChangeLayersColorComponent component, ref ComponentInit args)
    {
        foreach (var (layerName, val) in component.MapLayers)
        {
            val.Layer = layerName;
        }

        if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearanceComponent))
        {
            var dictionary = new Dictionary<string, Color>();
            foreach(var key in component.MapLayers.Keys)
            {
                dictionary.Add(key, Color.White);
            }
            _appearance.SetData(uid, StorageMapVisuals.InitLayers, new ColorLayerData(dictionary), appearanceComponent);
        }
    }

    private bool TryGetLayer(EntityUid ent, ChangeLayersColorComponent itemLayerColor, out string showLayer)
    {
        foreach (var mapLayerData in itemLayerColor.MapLayers.Values)
        {
            var count = _whitelistSystem.IsWhitelistPassOrNull(mapLayerData.Whitelist, ent);
            if (count)
            {
                showLayer = mapLayerData.Layer;
                return true;
            }
        }
        showLayer = string.Empty;
        return false;
    }
}
