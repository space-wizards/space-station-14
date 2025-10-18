using System.Linq;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Storage.Systems;

public sealed class ItemMapperSystem : SharedItemMapperSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

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
            component.RSIPath ??= sprite.BaseRSI!.Path;
        }
    }

    private void OnAppearance(EntityUid uid, ItemMapperComponent component, ref AppearanceChangeEvent args)
    {
        if (TryComp<SpriteComponent>(uid, out var spriteComponent))
        {
            if (component.SpriteLayers.Count == 0)
            {
                InitLayers((uid, component, spriteComponent, args.Component));
            }

            EnableLayers((uid, component, spriteComponent, args.Component));
        }
    }

    private void InitLayers(Entity<ItemMapperComponent, SpriteComponent, AppearanceComponent> ent)
    {
        var (owner, component, spriteComponent, appearance) = ent;
        if (!_appearance.TryGetData<ShowLayerData>(owner, StorageMapVisuals.InitLayers, out var wrapper, appearance))
            return;

        component.SpriteLayers.AddRange(wrapper.QueuedEntities);

        foreach (var sprite in component.SpriteLayers)
        {
            _sprite.LayerMapReserve((owner, spriteComponent), sprite);
            _sprite.LayerSetSprite((owner, spriteComponent), sprite, new SpriteSpecifier.Rsi(component.RSIPath!.Value, sprite));
            _sprite.LayerSetVisible((owner, spriteComponent), sprite, false);
        }
    }

    private void EnableLayers(Entity<ItemMapperComponent, SpriteComponent, AppearanceComponent> ent)
    {
        var (owner, component, spriteComponent, appearance) = ent;
        if (!_appearance.TryGetData<ShowLayerData>(owner, StorageMapVisuals.LayerChanged, out var wrapper, appearance))
            return;

        foreach (var layerName in component.SpriteLayers)
        {
            var show = wrapper.QueuedEntities.Contains(layerName);
            _sprite.LayerSetVisible((owner, spriteComponent), layerName, show);
        }
    }
}
