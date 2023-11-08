using Robust.Shared.GameObjects;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Content.Shared.Power.Substation;

namespace Content.Client.Power.Substation;

public sealed class SubstationVisualsSystem : VisualizerSystem<SubstationVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, SubstationVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || !args.Sprite.LayerMapTryGet(component.LayerMap, out var layer))
            return;

		if(args.AppearanceData.TryGetValue(SubstationVisuals.Screen, out var stateObject)
			&& stateObject is SubstationIntegrityState
			&& component.IntegrityStates.TryGetValue((SubstationIntegrityState)stateObject, out var state))
		{
			args.Sprite.LayerSetState(layer, new RSI.StateId(state));
		}
    }
}
