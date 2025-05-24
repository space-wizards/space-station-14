using Content.Shared.Storage;
using Robust.Client.GameObjects;

namespace Content.Client.Storage.Visualizers;

public sealed class EntityStorageVisualizerSystem : VisualizerSystem<EntityStorageVisualsComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntityStorageVisualsComponent, ComponentInit>(OnComponentInit);
    }

    /// <summary>
    /// Sets the base sprite to this layer. Exists to make the inheritance tree less boilerplate-y.
    /// </summary>
    private void OnComponentInit(EntityUid uid, EntityStorageVisualsComponent comp, ComponentInit args)
    {
        if (comp.StateBaseClosed == null)
            return;

        comp.StateBaseOpen ??= comp.StateBaseClosed;
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.LayerSetRsiState((uid, sprite), StorageVisualLayers.Base, comp.StateBaseClosed);
    }

    protected override void OnAppearanceChange(EntityUid uid, EntityStorageVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
        || !AppearanceSystem.TryGetData<bool>(uid, StorageVisuals.Open, out var open, args.Component))
            return;

        // Open/Closed state for the storage entity.
        if (_sprite.LayerMapTryGet((uid, args.Sprite), StorageVisualLayers.Door, out _, false))
        {
            if (open)
            {
                if (comp.OpenDrawDepth != null)
                    _sprite.SetDrawDepth((uid, args.Sprite), comp.OpenDrawDepth.Value);

                if (comp.StateDoorOpen != null)
                {
                    _sprite.LayerSetRsiState((uid, args.Sprite), StorageVisualLayers.Door, comp.StateDoorOpen);
                    _sprite.LayerSetVisible((uid, args.Sprite), StorageVisualLayers.Door, true);
                }
                else
                {
                    _sprite.LayerSetVisible((uid, args.Sprite), StorageVisualLayers.Door, false);
                }

                if (comp.StateBaseOpen != null)
                    _sprite.LayerSetRsiState((uid, args.Sprite), StorageVisualLayers.Base, comp.StateBaseOpen);
            }
            else
            {
                if (comp.ClosedDrawDepth != null)
                    _sprite.SetDrawDepth((uid, args.Sprite), comp.ClosedDrawDepth.Value);

                if (comp.StateDoorClosed != null)
                {
                    _sprite.LayerSetRsiState((uid, args.Sprite), StorageVisualLayers.Door, comp.StateDoorClosed);
                    _sprite.LayerSetVisible((uid, args.Sprite), StorageVisualLayers.Door, true);
                }
                else
                    _sprite.LayerSetVisible((uid, args.Sprite), StorageVisualLayers.Door, false);

                if (comp.StateBaseClosed != null)
                    _sprite.LayerSetRsiState((uid, args.Sprite), StorageVisualLayers.Base, comp.StateBaseClosed);
            }
        }
    }
}

public enum StorageVisualLayers : byte
{
    Base,
    Door
}
