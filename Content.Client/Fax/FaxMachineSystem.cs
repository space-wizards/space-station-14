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

        if (args.Component.TryGetData(PowerDeviceVisuals.Powered, out bool powered) &&
            args.Sprite.LayerMapTryGet(PowerDeviceVisualLayers.Powered, out var poweredLayer))
        {
            args.Sprite.LayerSetVisible(poweredLayer, powered);
        }

        if (args.Component.TryGetData(FaxMachineVisuals.BaseState, out FaxMachineVisualState baseState) &&
            args.Sprite.LayerMapTryGet(FaxMachineVisualLayers.Base, out var baseLayer))
        {
            string state = component.NormalState;
            if (baseState == FaxMachineVisualState.Inserting)
                state = component.InsertingState;
            if (baseState == FaxMachineVisualState.Printing)
                state = component.PrintState;

            args.Sprite.LayerSetAnimationTime(baseLayer, 0f);
            args.Sprite.LayerSetState(baseLayer, state);
        }
    }
}
