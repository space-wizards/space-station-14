using Content.Shared.Implants;
using Content.Shared.Storage.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Implants;

public sealed class ImplanterVisualsSystem : SharedImplanterVisualSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ImplanterVisualsComponent, AppearanceChangeEvent>(OnAppearance);
    }

    private void OnAppearance(EntityUid uid, ImplanterVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (TryComp<SpriteComponent>(uid, out var spriteComponent))
        {
            if (component.SpriteLayers.Count == 0)
            {
                InitLayers((uid, component, spriteComponent, args.Component));
            }

            UpdateLayers((uid, component, spriteComponent, args.Component));
        }
    }

    private void InitLayers((EntityUid uid, ImplanterVisualsComponent component, SpriteComponent spriteComponent, AppearanceComponent Component) ent)
    {
        var (owner, component, spriteComponent, appearance) = ent;
        if (!_appearance.TryGetData<ColorLayerData>(owner, StorageMapVisuals.InitLayers, out var wrapper, appearance))
            return;

        component.SpriteLayers.AddRange(wrapper.QueuedEntities.Keys);

        foreach (var layerName in component.SpriteLayers)
        {
            spriteComponent.LayerSetColor(layerName, wrapper.QueuedEntities[layerName]);
        }
    }

    private void UpdateLayers((EntityUid uid, ImplanterVisualsComponent component, SpriteComponent spriteComponent, AppearanceComponent Component) ent)
    {
        var (owner, component, spriteComponent, appearance) = ent;
        if (!_appearance.TryGetData<ColorLayerData>(owner, StorageMapVisuals.LayerChanged, out var wrapper, appearance))
            return;

        foreach (var layerName in component.SpriteLayers)
        {
            if(wrapper.QueuedEntities.ContainsKey(layerName))
                spriteComponent.LayerSetColor(layerName, wrapper.QueuedEntities[layerName]);
        }
    }
}
