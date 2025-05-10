using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client.Storage.Systems;

/// <inheritdoc/>
public sealed class ItemChangeLayerColorSystem : SharedItemChangeLayerColorSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangeLayersColorComponent, AppearanceChangeEvent>(OnAppearance);
    }

    private void OnAppearance(Entity<ChangeLayersColorComponent> ent, ref AppearanceChangeEvent args)
    {
        if (TryComp<SpriteComponent>(ent, out var spriteComponent))
        {
            if (ent.Comp.SpriteLayers.Count == 0)
            {
                InitLayers((ent, ent.Comp, spriteComponent, args.Component));
            }

            UpdateLayers((ent, ent.Comp, spriteComponent, args.Component));
        }
    }

    private void InitLayers((EntityUid uid, ChangeLayersColorComponent component, SpriteComponent spriteComponent, AppearanceComponent Component) ent)
    {
        var (owner, component, spriteComponent, appearance) = ent;
        if (!_appearance.TryGetData<ColorLayerData>(owner, StorageMapVisuals.InitLayers, out var wrapper, appearance))
            return;

        foreach (var nc in wrapper.LayersColors)
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
            foreach (var nc in wrapper.LayersColors)
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
