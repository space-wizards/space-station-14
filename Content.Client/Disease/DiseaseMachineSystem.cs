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
        protected override void OnAppearanceChange(EntityUid uid, DiseaseMachineVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (TryComp(uid, out SpriteComponent? sprite)
                && args.Component.TryGetData(DiseaseMachineVisuals.IsOn, out bool isOn)
                && args.Component.TryGetData(DiseaseMachineVisuals.IsRunning, out bool isRunning))
            {
                var state = isRunning ? component.RunningState : component.IdleState;
                sprite.LayerSetVisible(DiseaseMachineVisualLayers.IsOn, isOn);
                sprite.LayerSetState(DiseaseMachineVisualLayers.IsRunning, state);
            }
        }
    }
}
public enum DiseaseMachineVisualLayers : byte
{
    IsOn,
    IsRunning
}
