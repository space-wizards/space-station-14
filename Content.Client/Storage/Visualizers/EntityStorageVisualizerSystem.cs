using Content.Shared.SprayPainter.Prototypes;
using Content.Shared.Storage;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Storage.Visualizers;

public sealed class EntityStorageVisualizerSystem : VisualizerSystem<EntityStorageVisualsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

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

        comp.StateBaseOpen ??= comp.StateBaseClosed ?? comp.StateDoorClosed;
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.LayerSetState(StorageVisualLayers.Base, comp.StateBaseClosed);
        sprite.LayerSetState(StorageVisualLayers.Door, comp.StateDoorClosed ?? comp.StateBaseClosed);
    }

    protected override void OnAppearanceChange(EntityUid uid, EntityStorageVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
        || !AppearanceSystem.TryGetData<bool>(uid, StorageVisuals.Open, out var open, args.Component))
            return;

        if (AppearanceSystem.TryGetData<string>(uid, PaintableVisuals.BaseRSI, out var prototype1, args.Component))
        {
            var proto = Spawn(prototype1);

            if (TryComp<SpriteComponent>(proto, out var sprite))
                foreach (var layer in args.Sprite.AllLayers)
                {
                    if (layer.RsiState.Name == "paper")
                        continue;

                    layer.Rsi = sprite.BaseRSI;
                }

            Del(proto);
        } else if (AppearanceSystem.TryGetData<string>(uid, PaintableVisuals.LockerRSI, out var prototype2, args.Component))
        {
            var proto = Spawn(prototype2);

            if (TryComp<EntityStorageVisualsComponent>(proto, out var visuals))
            {
                args.Sprite.LayerSetState(StorageVisualLayers.Base, visuals.StateBaseClosed);
                args.Sprite.LayerSetState(StorageVisualLayers.Door, visuals.StateDoorClosed);
            }

            Del(proto);
            return;
        }

        // Open/Closed state for the storage entity.
        if (args.Sprite.LayerMapTryGet(StorageVisualLayers.Door, out _))
        {
            if (open)
            {
                if (comp.OpenDrawDepth != null)
                    args.Sprite.DrawDepth = comp.OpenDrawDepth.Value;

                if (comp.StateDoorOpen != null)
                {
                    args.Sprite.LayerSetState(StorageVisualLayers.Door, comp.StateDoorOpen);
                    args.Sprite.LayerSetVisible(StorageVisualLayers.Door, true);
                }
                else
                {
                    args.Sprite.LayerSetVisible(StorageVisualLayers.Door, false);
                }

                if (comp.StateBaseOpen != null)
                    args.Sprite.LayerSetState(StorageVisualLayers.Base, comp.StateBaseOpen);
            }
            else
            {
                if (comp.ClosedDrawDepth != null)
                    args.Sprite.DrawDepth = comp.ClosedDrawDepth.Value;

                if (comp.StateDoorClosed != null)
                {
                    args.Sprite.LayerSetState(StorageVisualLayers.Door, comp.StateDoorClosed);
                    args.Sprite.LayerSetVisible(StorageVisualLayers.Door, true);
                }
                else
                    args.Sprite.LayerSetVisible(StorageVisualLayers.Door, false);

                if (comp.StateBaseClosed != null)
                    args.Sprite.LayerSetState(StorageVisualLayers.Base, comp.StateBaseClosed);
            }
        }
    }
}

public enum StorageVisualLayers : byte
{
    Base,
    Door
}
