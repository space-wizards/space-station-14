using Content.Shared.SprayPainter.Prototypes;
using Content.Shared.Storage;
using Robust.Client.GameObjects;

namespace Content.Client.Storage.Visualizers;

public sealed partial class EntityStorageVisualizerSystem : VisualizerSystem<EntityStorageVisualsComponent>
{
    /// <summary>
    /// Sets the base sprite to this layer. Exists to make the inheritance tree less boilerplate-y.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnComponentInit(Entity<EntityStorageVisualsComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.StateBaseClosed == null)
            return;

        ent.Comp.StateBaseOpen ??= ent.Comp.StateBaseClosed;
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        SpriteSystem.LayerSetRsiState((ent, sprite), StorageVisualLayers.Base, ent.Comp.StateBaseClosed);
    }

    /// <inheritdoc/>
    protected override void OnAppearanceChange(EntityUid uid,
        EntityStorageVisualsComponent comp,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
            || !args.TryGetData<bool>(StorageVisuals.Open, out var open))
            return;

        var forceRedrawBase = false;
        if (args.TryGetData<string>(PaintableVisuals.Prototype, out var prototype))
        {
            if (ProtoMan.Resolve(prototype, out var proto))
            {
                if (proto.TryComp(out SpriteComponent? sprite, Factory))
                {
                    SpriteSystem.SetBaseRsi((uid, args.Sprite), sprite.BaseRSI);
                }
                if (proto.TryComp(out EntityStorageVisualsComponent? visuals, Factory))
                {
                    comp.StateBaseOpen = visuals.StateBaseOpen;
                    comp.StateBaseClosed = visuals.StateBaseClosed;
                    comp.StateDoorOpen = visuals.StateDoorOpen;
                    comp.StateDoorClosed = visuals.StateDoorClosed;
                    forceRedrawBase = true;
                }
            }
        }

        // Open/Closed state for the storage entity.
        if (!SpriteSystem.LayerMapTryGet((uid, args.Sprite), StorageVisualLayers.Door, out _, false))
            return;

        if (open)
        {
            if (comp.OpenDrawDepth != null)
                SpriteSystem.SetDrawDepth((uid, args.Sprite), comp.OpenDrawDepth.Value);

            if (comp.StateDoorOpen != null)
            {
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), StorageVisualLayers.Door, comp.StateDoorOpen);
                SpriteSystem.LayerSetVisible((uid, args.Sprite), StorageVisualLayers.Door, true);
            }
            else
            {
                SpriteSystem.LayerSetVisible((uid, args.Sprite), StorageVisualLayers.Door, false);
            }

            if (comp.StateBaseOpen != null)
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), StorageVisualLayers.Base, comp.StateBaseOpen);
            else if (forceRedrawBase && comp.StateBaseClosed != null)
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), StorageVisualLayers.Base, comp.StateBaseClosed);
        }
        else
        {
            if (comp.ClosedDrawDepth != null)
                SpriteSystem.SetDrawDepth((uid, args.Sprite), comp.ClosedDrawDepth.Value);

            if (comp.StateDoorClosed != null)
            {
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), StorageVisualLayers.Door, comp.StateDoorClosed);
                SpriteSystem.LayerSetVisible((uid, args.Sprite), StorageVisualLayers.Door, true);
            }
            else
                SpriteSystem.LayerSetVisible((uid, args.Sprite), StorageVisualLayers.Door, false);

            if (comp.StateBaseClosed != null)
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), StorageVisualLayers.Base, comp.StateBaseClosed);
            else if (forceRedrawBase && comp.StateBaseOpen != null)
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), StorageVisualLayers.Base, comp.StateBaseOpen);
        }
    }
}

public enum StorageVisualLayers : byte
{
    Base,
    Door
}
