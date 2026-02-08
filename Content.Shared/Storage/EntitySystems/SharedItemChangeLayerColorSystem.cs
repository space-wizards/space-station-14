using Content.Shared.Storage.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// System responsible for dynamically changing the color of specific sprite layers
/// on a container entity, based on the items inserted into or removed from its storage.
/// </summary>
/// <remarks>
/// The system listens for changes in storage containers and updates the appearance
/// of the parent entity using the <see cref="SharedAppearanceSystem"/>.
/// Target layers and conditions are defined using <see cref="ChangeLayersColorComponent"/> and
/// <see cref="ItemLayersColorComponent"/>, where the appropriate layer is selected
/// based on whitelist checks.
/// </remarks>
public abstract class SharedItemChangeLayerColorSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangeLayersColorComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<ChangeLayersColorComponent, EntInsertedIntoContainerMessage>(OnEntInsert);
        SubscribeLocalEvent<ChangeLayersColorComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
    }

    private void OnEntInsert(Entity<ChangeLayersColorComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!TryComp<AppearanceComponent>(ent.Owner, out var appearance))
            return;

        UpdateAppearance(ent, args.Entity, appearance);
    }

    private void OnEntRemoved(Entity<ChangeLayersColorComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!TryComp<AppearanceComponent>(ent.Owner, out var appearance))
            return;

        UpdateAppearance(ent, args.Entity, appearance);
    }

    private void UpdateAppearance(Entity<ChangeLayersColorComponent> mainComp, EntityUid item, AppearanceComponent appearance)
    {
        if (!TryComp<ItemLayersColorComponent>(item, out var itemLayerColor))
            return;

        var dict = new Dictionary<string, Color>();
        if (!TryGetLayer(item, mainComp.Comp, out var layer))
            return;

        dict.Add(layer, itemLayerColor.Color);
        _appearance.SetData(mainComp.Owner, LayerColorVisuals.LayerChanged, new ColorLayerData(dict), appearance);
    }

    private void OnComponentStartup(EntityUid uid, ChangeLayersColorComponent component, ref ComponentStartup args)
    {
        foreach (var (layerName, val) in component.MapLayers)
        {
            val.Layer = layerName;
        }

        if (TryComp(uid, out AppearanceComponent? appearanceComponent))
        {
            var dictionary = new Dictionary<string, Color>();
            foreach (var key in component.MapLayers.Keys)
            {
                dictionary.Add(key, Color.White);
            }
            _appearance.SetData(uid, LayerColorVisuals.InitLayers, new ColorLayerData(dictionary), appearanceComponent);
        }
    }

    private bool TryGetLayer(EntityUid ent, ChangeLayersColorComponent itemLayerColor, out string showLayer)
    {
        foreach (var mapLayerData in itemLayerColor.MapLayers.Values)
        {
            if (_whitelistSystem.IsWhitelistPassOrNull(mapLayerData.Whitelist, ent))
            {
                showLayer = mapLayerData.Layer;
                return true;
            }
        }
        showLayer = string.Empty;
        return false;
    }
}
