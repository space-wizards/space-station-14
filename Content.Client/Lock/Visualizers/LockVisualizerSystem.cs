using Content.Shared.Storage;
using Content.Shared.Lock;
using Robust.Client.GameObjects;

namespace Content.Client.Lock.Visualizers;

public sealed class LockVisualizerSystem : VisualizerSystem<LockVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, LockVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
            || !args.Sprite.LayerExists(LockVisualLayers.Lock))
            return;

        // Lock state for the entity. TODO: It needs some organization, it doesn't look quite good
        if (!AppearanceSystem.TryGetData<bool>(uid, LockVisuals.Locked, out var locked, args.Component))
            locked = true;

        var isStorage = AppearanceSystem.TryGetData<bool>(uid, StorageVisuals.Open, out var open, args.Component);
        var unlockedNotNull = comp.StateUnlocked != null;

        if (isStorage)
        {
            args.Sprite.LayerSetVisible(LockVisualLayers.Lock, !open);
        }
        else if (!unlockedNotNull)
            args.Sprite.LayerSetVisible(LockVisualLayers.Lock, locked);

        if (!isStorage && unlockedNotNull)
        {
            args.Sprite.LayerSetState(LockVisualLayers.Lock, locked ? comp.StateLocked : comp.StateUnlocked);
        }
        else if (!open && isStorage)
        {
            args.Sprite.LayerSetState(LockVisualLayers.Lock, locked ? comp.StateLocked : comp.StateStorageUnlocked);
        }
    }
}

public enum LockVisualLayers : byte
{
    Lock
}
