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
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        protected override void OnAppearanceChange(EntityUid uid, PortableScrubberVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            if (_appearance.TryGetData(uid, PortableScrubberVisuals.IsFull, out bool isFull, args.Component)
                && _appearance.TryGetData(uid, PortableScrubberVisuals.IsRunning, out bool isRunning, args.Component))
            {
                var runningState = isRunning ? component.RunningState : component.IdleState;
                args.Sprite.LayerSetState(PortableScrubberVisualLayers.IsRunning, runningState);

                var fullState = isFull ? component.FullState : component.ReadyState;
                args.Sprite.LayerSetState(PowerDeviceVisualLayers.Powered, fullState);
            }

            if (_appearance.TryGetData(uid, PortableScrubberVisuals.IsDraining, out bool isDraining, args.Component))
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
