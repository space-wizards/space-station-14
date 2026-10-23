using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client.Storage.Systems;

/// <inheritdoc/>
public sealed partial class ItemChangeLayerColorSystem : SharedItemChangeLayerColorSystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _spriteSystem = default!;

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
            _spriteSystem.LayerSetColor((owner, spriteComponent), nc.LayerName, nc.Color);
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
                    _spriteSystem.LayerSetColor((owner, spriteComponent), layerName, nc.Color);
                    break;
                }
            }
        }
    }
}
