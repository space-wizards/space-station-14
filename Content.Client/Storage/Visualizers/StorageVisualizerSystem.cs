using Content.Shared.Storage;
using Robust.Client.GameObjects;

namespace Content.Client.Storage.Visualizers;

public sealed class EntityStorageVisualizerSystem : VisualizerSystem<EntityStorageVisualsComponent>
{
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
        if (comp.StateBase == null)
            return;

        comp.StateBaseOpen ??= comp.StateBase;
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.LayerSetState(StorageVisualLayers.Base, comp.StateBase);
    }

    protected override void OnAppearanceChange(EntityUid uid, EntityStorageVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
        || !AppearanceSystem.TryGetData<bool>(uid, StorageVisuals.Open, out var open, args.Component))
            return;

        // Open/Closed state for the storage entity.
        if (args.Sprite.LayerMapTryGet(StorageVisualLayers.Door, out _))
        {
            if (open)
            {
                args.Sprite.LayerSetVisible(StorageVisualLayers.Door, true);
                if (comp.StateOpen != null)
                    args.Sprite.LayerSetState(StorageVisualLayers.Door, comp.StateOpen);

                if (comp.StateBaseOpen != null)
                    args.Sprite.LayerSetState(StorageVisualLayers.Base, comp.StateBaseOpen);
            }
            else
            {
                if (comp.StateClosed != null)
                {
                    args.Sprite.LayerSetState(StorageVisualLayers.Door, comp.StateClosed);
                    args.Sprite.LayerSetVisible(StorageVisualLayers.Door, true);
                }
                else
                    args.Sprite.LayerSetVisible(StorageVisualLayers.Door, false);

                if (comp.StateBase != null)
                    args.Sprite.LayerSetState(StorageVisualLayers.Base, comp.StateBase);
            }
        }

        // Lock state for the storage entity. TODO: Split into its own visualizer.
        if (AppearanceSystem.TryGetData<bool>(uid, StorageVisuals.CanLock, out var canLock, args.Component) && canLock)
        {
            if (!AppearanceSystem.TryGetData<bool>(uid, StorageVisuals.Locked, out var locked, args.Component))
                locked = true;

            args.Sprite.LayerSetVisible(StorageVisualLayers.Lock, !open);
            if (!open)
            {
                args.Sprite.LayerSetState(StorageVisualLayers.Lock, locked ? comp.StateLocked : comp.StateUnlocked);
            }
        }
    }
}

public enum StorageVisualLayers : byte
{
    Base,
    Door,
    Lock
}
