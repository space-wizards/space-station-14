using Robust.Client.GameObjects;
using Content.Shared.Disease;

namespace Content.Client.Disease
{
    /// <summary>
    /// Controls client-side visuals for the
    /// disease machines.
    /// </summary>
    public sealed class DiseaseMachineSystem : VisualizerSystem<DiseaseMachineVisualsComponent>
    {
        [Dependency] private readonly AppearanceSystem _appearance = default!;

        protected override void OnAppearanceChange(EntityUid uid, DiseaseMachineVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            if (_appearance.TryGetData(uid, DiseaseMachineVisuals.IsOn, out bool isOn, args.Component)
                && _appearance.TryGetData(uid, DiseaseMachineVisuals.IsRunning, out bool isRunning, args.Component))
            {
                var state = isRunning ? component.RunningState : component.IdleState;
                args.Sprite.LayerSetVisible(DiseaseMachineVisualLayers.IsOn, isOn);
                args.Sprite.LayerSetState(DiseaseMachineVisualLayers.IsRunning, state);
            }
        }
    }
}
public enum DiseaseMachineVisualLayers : byte
{
    IsOn,
    IsRunning
}
