using Content.Shared.SprayPainter.Prototypes;
using Content.Shared.Storage;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Storage.Visualizers;

public sealed class EntityStorageVisualizerSystem : VisualizerSystem<EntityStorageVisualsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
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

    protected override void OnAppearanceChange(EntityUid uid,
        EntityStorageVisualsComponent comp,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
            || !AppearanceSystem.TryGetData<bool>(uid, StorageVisuals.Open, out var open, args.Component))
            return;

        if (AppearanceSystem.TryGetData<string>(uid, PaintableVisuals.BaseRSI, out var prototype1, args.Component))
        {
            if (_prototypeManager.TryIndex(prototype1, out var proto) &&
                proto.TryGetComponent(out SpriteComponent? sprite, _componentFactory))
            {
                _sprite.SetBaseRsi((uid, args.Sprite), sprite.BaseRSI);
            }
        }

        bool forceRedrawBase = false;
        if (AppearanceSystem.TryGetData<string>(uid, PaintableVisuals.Prototype, out var prototype2, args.Component))
        {
            if (_prototypeManager.TryIndex(prototype2, out var proto) &&
                proto.TryGetComponent(out EntityStorageVisualsComponent? visuals, _componentFactory))
            {
                comp.StateBaseOpen = visuals.StateBaseOpen;
                comp.StateBaseClosed = visuals.StateBaseClosed;
                comp.StateDoorOpen = visuals.StateDoorOpen;
                comp.StateDoorClosed = visuals.StateDoorClosed;
                forceRedrawBase = true;
            }
        }

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
                else if (forceRedrawBase && comp.StateBaseClosed != null)
                    _sprite.LayerSetRsiState((uid, args.Sprite), StorageVisualLayers.Base, comp.StateBaseClosed);
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
                else if (forceRedrawBase && comp.StateBaseOpen != null)
                    _sprite.LayerSetRsiState((uid, args.Sprite), StorageVisualLayers.Base, comp.StateBaseOpen);
            }
        }
    }
}

public enum StorageVisualLayers : byte
{
    Base,
    Door
}
