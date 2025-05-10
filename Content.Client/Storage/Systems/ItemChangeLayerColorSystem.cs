using Content.Shared.Implants;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client.Storage.Systems;

public sealed class ItemChangeLayerColorSystem : SharedItemChangeLayerColorSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangeLayersColorComponent, AppearanceChangeEvent>(OnAppearance);
    }

    private void OnAppearance(EntityUid uid, ChangeLayersColorComponent component, ref AppearanceChangeEvent args)
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

    private void InitLayers((EntityUid uid, ChangeLayersColorComponent component, SpriteComponent spriteComponent, AppearanceComponent Component) ent)
    {
        var (owner, component, spriteComponent, appearance) = ent;
        if (!_appearance.TryGetData<ColorLayerData>(owner, StorageMapVisuals.InitLayers, out var wrapper, appearance))
            return;

        foreach (var nc in wrapper.QueuedEntities)
        {
            component.SpriteLayers.Add(nc.LayerName);
            spriteComponent.LayerSetColor(nc.LayerName, nc.Color);
        }
    }

    private void UpdateLayers((EntityUid uid, ChangeLayersColorComponent component, SpriteComponent spriteComponent, AppearanceComponent Component) ent)
    {
        var (owner, component, spriteComponent, appearance) = ent;
        if (!_appearance.TryGetData<ColorLayerData>(owner, StorageMapVisuals.LayerChanged, out var wrapper, appearance))
            return;

        foreach (var layerName in component.SpriteLayers)
        {
            foreach (var nc in wrapper.QueuedEntities)
            {
                if (nc.LayerName == layerName)
                {
                    spriteComponent.LayerSetColor(layerName, nc.Color);
                    break;
                }
            }
        }
    }
}
