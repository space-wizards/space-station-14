using System.Linq;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Storage.Systems;

public sealed class ItemMapperSystem : SharedItemMapperSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ItemMapperComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ItemMapperComponent, AppearanceChangeEvent>(OnAppearance);
    }

    private void OnStartup(EntityUid uid, ItemMapperComponent component, ComponentStartup args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            component.RSIPath ??= sprite.BaseRSI!.Path!;
        }
    }

    private void OnAppearance(EntityUid uid, ItemMapperComponent component, ref AppearanceChangeEvent args)
    {
        if (TryComp<SpriteComponent>(component.Owner, out var spriteComponent))
        {
            if (component.SpriteLayers.Count == 0)
            {
                InitLayers(component, spriteComponent, args.Component);
            }

            EnableLayers(component, spriteComponent, args.Component);
        }
    }

    private void InitLayers(ItemMapperComponent component, SpriteComponent spriteComponent, AppearanceComponent appearance)
    {
        if (!_appearance.TryGetData<ShowLayerData>(appearance.Owner, StorageMapVisuals.InitLayers, out var wrapper, appearance))
            return;

        component.SpriteLayers.AddRange(wrapper.QueuedEntities);

        foreach (var sprite in component.SpriteLayers)
        {
            spriteComponent.LayerMapReserveBlank(sprite);
            spriteComponent.LayerSetSprite(sprite, new SpriteSpecifier.Rsi(component.RSIPath!, sprite));
            spriteComponent.LayerSetVisible(sprite, false);
        }
    }

    private void EnableLayers(ItemMapperComponent component, SpriteComponent spriteComponent, AppearanceComponent appearance)
    {
        if (!_appearance.TryGetData<ShowLayerData>(appearance.Owner, StorageMapVisuals.LayerChanged, out var wrapper, appearance))
            return;

        foreach (var layerName in component.SpriteLayers)
        {
            var show = wrapper.QueuedEntities.Contains(layerName);
            spriteComponent.LayerSetVisible(layerName, show);
        }
    }
}
