using Content.Shared.Storage;
using Content.Shared.Lock;
using Robust.Client.GameObjects;

namespace Content.Client.Lock.Visualizers;

public sealed partial class LockVisualizerSystem : VisualizerSystem<LockVisualsComponent>
{
    /// <inheritdoc/>
    protected override void OnAppearanceChange(EntityUid uid, LockVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
            || !args.TryGetData<bool>(LockVisuals.Locked, out var locked))
            return;

        var unlockedStateExist = args.Sprite.BaseRSI?.TryGetState(comp.StateUnlocked, out _);

        if (args.TryGetData<bool>(StorageVisuals.Open, out var open))
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), LockVisualLayers.Lock, !open);
        }
        else if (!(bool)unlockedStateExist!)
            SpriteSystem.LayerSetVisible((uid, args.Sprite), LockVisualLayers.Lock, locked);

        if (!open && (bool)unlockedStateExist!)
        {
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), LockVisualLayers.Lock, locked ? comp.StateLocked : comp.StateUnlocked);
        }
    }
}

public enum LockVisualLayers : byte
{
    Lock
}
