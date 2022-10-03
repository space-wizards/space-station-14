using Content.Client.Power;
using Content.Shared.Fax;
using Content.Shared.Power;
using Robust.Client.GameObjects;

namespace Content.Client.Fax;

public sealed class FaxMachineSystem : VisualizerSystem<FaxMachineVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, FaxMachineVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.Component.TryGetData(PowerDeviceVisuals.Powered, out bool powered) ||
            !args.Component.TryGetData(FaxMachineVisuals.VisualState, out FaxMachineVisualState visualState) ||
            !args.Sprite.LayerMapTryGet(FaxMachineVisualLayers.Base, out var baseLayer))
            return;

        var layerState = visualState switch
        {
            FaxMachineVisualState.Inserting => component.InsertingState,
            FaxMachineVisualState.Printing => component.PrintState,
            _ => powered ? component.IdleState : component.OffState,
        };

        args.Sprite.LayerSetAnimationTime(baseLayer, 0f);
        args.Sprite.LayerSetState(baseLayer, layerState);
    }
}
