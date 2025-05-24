using Robust.Client.GameObjects;
using Content.Shared.Atmos.Visuals;
using Content.Client.Power;

namespace Content.Client.Atmos.Visualizers
{
    /// <summary>
    /// Controls client-side visuals for portable scrubbers.
    /// </summary>
    public sealed class PortableScrubberSystem : VisualizerSystem<PortableScrubberVisualsComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, PortableScrubberVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            if (AppearanceSystem.TryGetData<bool>(uid, PortableScrubberVisuals.IsFull, out var isFull, args.Component)
                && AppearanceSystem.TryGetData<bool>(uid, PortableScrubberVisuals.IsRunning, out var isRunning, args.Component))
            {
                var runningState = isRunning ? component.RunningState : component.IdleState;
                args.Sprite.LayerSetState(PortableScrubberVisualLayers.IsRunning, runningState);

                var fullState = isFull ? component.FullState : component.ReadyState;
                args.Sprite.LayerSetState(PowerDeviceVisualLayers.Powered, fullState);
            }

            if (AppearanceSystem.TryGetData<bool>(uid, PortableScrubberVisuals.IsDraining, out var isDraining, args.Component))
            {
                args.Sprite.LayerSetVisible(PortableScrubberVisualLayers.IsDraining, isDraining);
            }
        }
    }
}
public enum PortableScrubberVisualLayers : byte
{
    IsRunning,

    IsDraining
}
