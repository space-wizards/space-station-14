using Content.Shared.Storage;
using Content.Shared.Lock;
using Robust.Client.GameObjects;

namespace Content.Client.Lock.Visualizers;

public sealed class LockVisualizerSystem : VisualizerSystem<LockComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, LockComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
            || !args.Sprite.LayerExists(LockVisualLayers.Lock))
            return;

        // Lock state for the entity.
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
