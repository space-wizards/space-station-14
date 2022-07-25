using Content.Shared.MachineLinking;
using Robust.Client.GameObjects;

public sealed class TwoWayLeverVisualizerSystem : VisualizerSystem<TwoWayLeverVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, TwoWayLeverVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite != null
           && args.Component.TryGetData(TwoWayLeverVisuals.State, out TwoWayLeverState state))
        {
            var texture = state switch
            {
                TwoWayLeverState.Middle => component.StateOff,
                TwoWayLeverState.Right => component.StateForward,
                TwoWayLeverState.Left => component.StateReversed,
                _ => component.StateOff
            };

            args.Sprite.LayerSetState(0, texture);
        }
    }
}
