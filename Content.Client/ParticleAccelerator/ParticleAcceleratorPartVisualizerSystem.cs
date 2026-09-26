using Content.Shared.Singularity.Components;
using Robust.Client.GameObjects;

namespace Content.Client.ParticleAccelerator;

public sealed partial class ParticleAcceleratorPartVisualizerSystem : VisualizerSystem<ParticleAcceleratorPartVisualsComponent>
{
    /// <inheritdoc/>
    protected override void OnAppearanceChange(EntityUid uid, ParticleAcceleratorPartVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!SpriteSystem.LayerMapTryGet((uid, args.Sprite), ParticleAcceleratorVisualLayers.Unlit, out var index, false))
            return;

        if (!args.TryGetData<ParticleAcceleratorVisualState>(ParticleAcceleratorVisuals.VisualState, out var state))
        {
            state = ParticleAcceleratorVisualState.Unpowered;
        }

        if (state != ParticleAcceleratorVisualState.Unpowered)
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), index, true);
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), index, comp.StateBase + comp.StatesSuffixes[state]);
        }
        else
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), index, false);
        }
    }
}
