using System.Linq;
using Content.Shared.Singularity.Components;
using Robust.Client.GameObjects;

namespace Content.Client.ParticleAccelerator;

public sealed class ParticleAcceleratorPartVisualizerSystem : VisualizerSystem<ParticleAcceleratorPartVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ParticleAcceleratorPartVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        
        if (!AppearanceSystem.TryGetData<ParticleAcceleratorVisualState>(uid, ParticleAcceleratorVisuals.VisualState, out var state, args.Component))
        {
            state = ParticleAcceleratorVisualState.Unpowered;
        }

        if (state != ParticleAcceleratorVisualState.Unpowered)
        {
            args.Sprite.LayerSetVisible(ParticleAcceleratorVisualLayers.Unlit, true);
            args.Sprite.LayerSetState(ParticleAcceleratorVisualLayers.Unlit, comp.StateBase + comp.StatesSuffixes[state]);
        }
        else
        {
            args.Sprite.LayerSetVisible(ParticleAcceleratorVisualLayers.Unlit, false);
        }
    }
}
