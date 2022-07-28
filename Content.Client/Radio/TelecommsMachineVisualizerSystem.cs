using Content.Shared.Radio;
using Robust.Client.GameObjects;

namespace Content.Client.Radio;

public sealed class TelecommsMachineVisualizerSystem : VisualizerSystem<TelecommsMachineVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, TelecommsMachineVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite) ||
            !args.Component.TryGetData(TelecommsMachineVisuals.IsOn, out bool isOn) ||
            !args.Component.TryGetData(TelecommsMachineVisuals.IsTransmiting, out bool isTransmitting)) return;
        var state = isOn ? component.OnState : component.OffState;
        sprite.LayerSetState(TelecommsMachineVisualLayers.Machine, state);
        if (component.TXRXState != null && isTransmitting)
        {
            sprite.LayerSetState(TelecommsMachineVisualLayers.Machine, component.TXRXState);
        }
    }

}

public enum TelecommsMachineVisualLayers
{
    // we change our own state
    Machine
}
