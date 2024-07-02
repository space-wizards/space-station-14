using Content.Shared.Power;
using Content.Shared.Substation;
using Robust.Client.GameObjects;

namespace Content.Client.Power.Substation;
public sealed class SubstationVisualizerSystem : VisualizerSystem<SubstationVisualizerComponent>
{
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    protected override void OnAppearanceChange(EntityUid uid, SubstationVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<SubstationChargeState>(uid, SubstationVisuals.LastChargeState, out var state, args.Component))
            state = SubstationChargeState.Full;

        switch (state)
        {
            case SubstationChargeState.Dead:
                args.Sprite.LayerSetState(SubstationVisualLayers.Charge, $"dead");
                args.Sprite.LayerSetVisible(SubstationVisualLayers.Screen, false);
                _lights.SetEnabled(uid, false);
                break;
            case SubstationChargeState.Discharging:
                args.Sprite.LayerSetState(SubstationVisualLayers.Charge, $"dead");
                args.Sprite.LayerSetVisible(SubstationVisualLayers.Screen, true);
                _lights.SetEnabled(uid, true);
                break;
            case SubstationChargeState.Charging:
                args.Sprite.LayerSetState(SubstationVisualLayers.Charge, $"charging");
                args.Sprite.LayerSetVisible(SubstationVisualLayers.Screen, true);
                _lights.SetEnabled(uid, true);
                break;
            case SubstationChargeState.Full:
                args.Sprite.LayerSetState(SubstationVisualLayers.Charge, $"full");
                args.Sprite.LayerSetVisible(SubstationVisualLayers.Screen, true);
                _lights.SetEnabled(uid, true);
                break;
        }
    }
}

enum SubstationVisualLayers : byte
{
    Charge,
    Screen,
}