using Content.Shared.Singularity.Components;
using Robust.Client.GameObjects;

namespace Content.Client.ParticleAccelerator;

public sealed class ParticleAcceleratorPartVisualizerSystem : VisualizerSystem<ParticleAcceleratorPartVisualsComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    protected override void OnAppearanceChange(EntityUid uid, ParticleAcceleratorPartVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_sprite.LayerMapTryGet((uid, args.Sprite), ParticleAcceleratorVisualLayers.Unlit, out var index, false))
            return;

        if (!AppearanceSystem.TryGetData<ParticleAcceleratorVisualState>(uid, ParticleAcceleratorVisuals.VisualState, out var state, args.Component))
        {
            state = ParticleAcceleratorVisualState.Unpowered;
        }

        if (state != ParticleAcceleratorVisualState.Unpowered)
        {
            _sprite.LayerSetVisible((uid, args.Sprite), index, true);
            _sprite.LayerSetRsiState((uid, args.Sprite), index, comp.StateBase + comp.StatesSuffixes[state]);
        }
        else
        {
            _sprite.LayerSetVisible((uid, args.Sprite), index, false);
        }
    }
}
