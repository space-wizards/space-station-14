using Content.Shared.Singularity.Components;
using Robust.Client.GameObjects;

namespace Content.Client.ParticleAccelerator;

public sealed class ParticleAcceleratorPartVisualizerSystem : VisualizerSystem<ParticleAcceleratorPartVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ParticleAcceleratorPartVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.Sprite.LayerMapTryGet(ParticleAcceleratorVisualLayers.Unlit, out var index))
            return;

        if (!AppearanceSystem.TryGetData<ParticleAcceleratorVisualState>(uid, ParticleAcceleratorVisuals.VisualState, out var state, args.Component))
        {
            state = ParticleAcceleratorVisualState.Unpowered;
        }

        if (state != ParticleAcceleratorVisualState.Unpowered)
        {
            args.Sprite.LayerSetVisible(index, true);
            args.Sprite.LayerSetState(index, comp.StateBase + comp.StatesSuffixes[state]);
        }
        else
        {
            args.Sprite.LayerSetVisible(index, false);
        }
    }
}
