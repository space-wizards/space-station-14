using Robust.Client.GameObjects;
using Content.Shared.Lathe;
using Content.Shared.Power;
using Content.Client.Power;

namespace Content.Client.Lathe
{
    public sealed class LatheSystem : VisualizerSystem<LatheVisualsComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, LatheVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            if (args.Component.TryGetData(PowerDeviceVisuals.Powered, out bool powered) &&
                args.Sprite.LayerMapTryGet(PowerDeviceVisualLayers.Powered, out _))
            {
                args.Sprite.LayerSetVisible(PowerDeviceVisualLayers.Powered, powered);
            }

            // Lathe specific stuff
            if (args.Component.TryGetData(LatheVisuals.IsRunning, out bool isRunning))
            {
                var state = isRunning ? component.RunningState : component.IdleState;
                args.Sprite.LayerSetAnimationTime(LatheVisualLayers.IsRunning, 0f);
                args.Sprite.LayerSetState(LatheVisualLayers.IsRunning, state);
            }

            if (args.Component.TryGetData(LatheVisuals.IsInserting, out bool isInserting)
                && args.Sprite.LayerMapTryGet(LatheVisualLayers.IsInserting, out var isInsertingLayer))
            {
                if (args.Component.TryGetData(LatheVisuals.InsertingColor, out Color color)
                    && !component.IgnoreColor)
                {
                    args.Sprite.LayerSetColor(isInsertingLayer, color);
                }

                args.Sprite.LayerSetAnimationTime(isInsertingLayer, 0f);
                args.Sprite.LayerSetVisible(isInsertingLayer, isInserting);
            }
        }
    }
}
public enum LatheVisualLayers : byte
{
    IsRunning,
    IsInserting
}
