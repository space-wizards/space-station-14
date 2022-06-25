using Content.Client.Atmos.Components;
using Content.Shared.Atmos.Visuals;
using Robust.Client.GameObjects;

namespace Content.Client.Atmos.EntitySystems;

public sealed class VentPumpVisualizerSystem : VisualizerSystem<VentPumpVisualsComponent>
{
    private string _offState = "vent_off";
    private string _inState = "vent_in";
    private string _outState = "vent_out";
    private string _weldedState = "vent_welded";

    protected override void OnAppearanceChange(EntityUid uid, VentPumpVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!args.Component.TryGetData<VentPumpState>(VentPumpVisuals.State, out var state))
            return;

        switch (state)
        {
            case VentPumpState.Off:
                sprite.LayerSetState(VentPumpVisualLayers.Vent, _offState);
                break;
            case VentPumpState.In:
                sprite.LayerSetState(VentPumpVisualLayers.Vent, _inState);
                break;
            case VentPumpState.Out:
                sprite.LayerSetState(VentPumpVisualLayers.Vent, _outState);
                break;
            case VentPumpState.Welded:
                sprite.LayerSetState(VentPumpVisualLayers.Vent, _weldedState);
                break;
            default:
                sprite.LayerSetState(VentPumpVisualLayers.Vent, _offState);
                break;
        }
    }
}

public enum VentPumpVisualLayers : byte
{
    Vent,
}
