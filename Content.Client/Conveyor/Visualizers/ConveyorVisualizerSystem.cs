using Robust.Client.GameObjects;
using Content.Shared.Conveyor;

namespace Content.Client.Conveyor.Visualizers;

public sealed class ConveyorVisualizerSystem : VisualizerSystem<ConveyorVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ConveyorVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite != null
            && args.Component.TryGetData(ConveyorVisuals.State, out ConveyorState state))
        {
            var texture = state switch
            {
                ConveyorState.Off => component.StateStopped,
                ConveyorState.Forward => component.StateRunning,
                ConveyorState.Reverse => component.StateReversed,
                _ => throw new ArgumentOutOfRangeException()
            };

            args.Sprite.LayerSetState(0, texture);
        }
    }
}
