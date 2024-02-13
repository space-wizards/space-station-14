using Content.Shared.Storage;
using Content.Shared.Lock;
using Robust.Client.GameObjects;

namespace Content.Client.Lock.Visualizers;

public sealed class LockVisualizerSystem : VisualizerSystem<LockComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LockComponent, ComponentInit>(OnComponentInit);
    }

    /// <summary>
    /// Sets the base sprite to this layer. Exists to make the inheritance tree less boilerplate-y.
    /// </summary>
    private void OnComponentInit(EntityUid uid, LockComponent comp, ComponentInit args)
    {
        if (comp.StateLocked == null)
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.LayerSetState(LockVisualLayers.Lock, comp.StateLocked);
    }

    protected override void OnAppearanceChange(EntityUid uid, LockComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // Lock state for the entity.
        if (!AppearanceSystem.TryGetData<bool>(uid, LockVisuals.CanLock, out var canLock, args.Component) || !canLock)
            return;
        if (!AppearanceSystem.TryGetData<bool>(uid, LockVisuals.Locked, out var locked, args.Component))
            locked = true;

        if (AppearanceSystem.TryGetData<bool>(uid, StorageVisuals.Open, out var open, args.Component))
        {
            args.Sprite.LayerSetVisible(LockVisualLayers.Lock, !open);
        }
        else
            args.Sprite.LayerSetVisible(LockVisualLayers.Lock, locked);

        if (!open)
        {
            args.Sprite.LayerSetState(LockVisualLayers.Lock, locked ? comp.StateLocked : comp.StateUnlocked);
        }
    }
}

public enum LockVisualLayers : byte
{
    Lock
}
