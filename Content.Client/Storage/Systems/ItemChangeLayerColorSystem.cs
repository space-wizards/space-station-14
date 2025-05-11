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
        if (TryComp<SpriteComponent>(ent.Owner, out var spriteComponent))
        {
            if (ent.Comp.SpriteLayers.Count == 0)
            {
                InitLayers((ent.Owner, ent.Comp, spriteComponent, args.Component));
            }

            UpdateLayers((ent.Owner, ent.Comp, spriteComponent, args.Component));
        }
    }

    private void InitLayers(Entity<ChangeLayersColorComponent, SpriteComponent, AppearanceComponent> ent)
    {
        var layerColorComponent = ent.Comp1;
        var spriteComponent = ent.Comp2;
        var appearance = ent.Comp3;
        var owner = ent.Owner;

        if (!_appearance.TryGetData<ColorLayerData>(owner, LayerColorVisuals.InitLayers, out var wrapper, appearance))
            return;

        foreach (var nc in wrapper.LayersColors)
        {
            layerColorComponent.SpriteLayers.Add(nc.LayerName);
            spriteComponent.LayerSetColor(nc.LayerName, nc.Color);
        }
    }

    private void UpdateLayers(Entity<ChangeLayersColorComponent, SpriteComponent, AppearanceComponent> ent)
    {
        var layerColorComponent = ent.Comp1;
        var spriteComponent = ent.Comp2;
        var appearance = ent.Comp3;
        var owner = ent.Owner;

        if (!_appearance.TryGetData<ColorLayerData>(owner, LayerColorVisuals.LayerChanged, out var wrapper, appearance))
            return;

        foreach (var layerName in layerColorComponent.SpriteLayers)
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
