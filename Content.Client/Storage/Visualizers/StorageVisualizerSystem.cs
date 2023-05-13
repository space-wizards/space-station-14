using Content.Shared.Storage;
using Robust.Client.GameObjects;

namespace Content.Client.Storage.Visualizers;

public sealed class StorageVisualizerSystem : VisualizerSystem<EntityStorageVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntityStorageVisualsComponent, ComponentInit>(OnInit);
    }

    /// <summary>
    /// Sets the base sprite to this layer. Exists to make the inheritance tree less boilerplate-y.
    /// </summary>
    private void OnInit(EntityUid uid, EntityStorageVisualsComponent comp, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (comp.StateBase != null)
        {
            sprite.LayerSetState(0, comp.StateBase);
        }

        if (comp.StateBaseAlt == null)
        {
            comp.StateBaseAlt ??= comp.StateBase;
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, EntityStorageVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if (!AppearanceSystem.TryGetData(uid, StorageVisuals.Open, out bool open, args.Component))
            return;

        if (args.Sprite.LayerMapTryGet(StorageVisualLayers.Door, out _))
        {
            args.Sprite.LayerSetVisible(StorageVisualLayers.Door, true);

            if (open)
            {
                if (comp.StateOpen != null)
                {
                    args.Sprite.LayerSetState(StorageVisualLayers.Door, comp.StateOpen);
                    args.Sprite.LayerSetVisible(StorageVisualLayers.Door, true);
                }

                if (comp.StateBaseAlt != null)
                    args.Sprite.LayerSetState(0, comp.StateBaseAlt);
            }
            else if (!open)
            {
                if (comp.StateClosed != null)
                    args.Sprite.LayerSetState(StorageVisualLayers.Door, comp.StateClosed);
                else
                {
                    args.Sprite.LayerSetVisible(StorageVisualLayers.Door, false);
                }

                if (comp.StateBase != null)
                    args.Sprite.LayerSetState(0, comp.StateBase);
            }
            else
            {
                args.Sprite.LayerSetVisible(StorageVisualLayers.Door, false);
            }
        }

        if (AppearanceSystem.TryGetData(uid, StorageVisuals.CanLock, out bool canLock, args.Component) && canLock)
        {
            if (!AppearanceSystem.TryGetData(uid, StorageVisuals.Locked, out bool locked, args.Component))
            {
                locked = true;
            }

            args.Sprite.LayerSetVisible(StorageVisualLayers.Lock, !open);
            if (!open)
            {
                args.Sprite.LayerSetState(StorageVisualLayers.Lock, locked ? "locked" : "unlocked");
            }
        }
    }
}

public enum StorageVisualLayers : byte
{
    Door,
    Lock
}
