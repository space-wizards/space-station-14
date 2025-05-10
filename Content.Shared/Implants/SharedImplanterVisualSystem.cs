using Content.Shared.Implants.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.Implants;

public class SharedImplanterVisualSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplanterVisualsComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ImplanterVisualsComponent, EntInsertedIntoContainerMessage>(OnEntInsert);
        SubscribeLocalEvent<ImplanterVisualsComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
    }

    private void OnEntInsert(EntityUid uid, ImplanterVisualsComponent comp, ref EntInsertedIntoContainerMessage args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        var ent = args.Entity;

        if (!TryComp<SubdermalImplantComponent>(ent, out var subcomp))
            return;

        UpdateAppearance(ent, subcomp, comp, appearance);
    }

    private void OnEntRemoved(EntityUid uid, ImplanterVisualsComponent comp, ref EntRemovedFromContainerMessage args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        var ent = args.Entity;

        if (!TryComp<SubdermalImplantComponent>(ent, out var subcomp))
            return;

        UpdateAppearance(ent, subcomp, comp, appearance);
    }

    private void UpdateAppearance(EntityUid parent, SubdermalImplantComponent entMovedComp, ImplanterVisualsComponent comp, AppearanceComponent appearance)
    {
        var dict = new Dictionary<string, Color>();
        if(!TryGetLayer(comp.Owner, comp, out var layer))
            return;

        dict.Add(layer, entMovedComp.Color);
        _appearance.SetData(parent, StorageMapVisuals.LayerChanged, new ColorLayerData(), appearance);
    }

    private void OnComponentInit(EntityUid uid, ImplanterVisualsComponent component, ref ComponentInit args)
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

    private bool TryGetLayer(EntityUid ent, ImplanterVisualsComponent itemMapper, out string showLayer)
    {
        string result = "";
        foreach (var mapLayerData in itemMapper.MapLayers.Values)
        {
            var count = _whitelistSystem.IsWhitelistPassOrNull(mapLayerData.Whitelist, ent);
            if (count)
            {
                result = mapLayerData.Layer;
                break;
            }
        }

        showLayer = result;
        return true;
    }
}
