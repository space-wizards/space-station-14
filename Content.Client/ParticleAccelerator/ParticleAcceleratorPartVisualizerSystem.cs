using System.Linq;
using Content.Shared.Singularity.Components;
using Robust.Client.GameObjects;

namespace Content.Client.ParticleAccelerator;

public sealed class ParticleAcceleratorPartVisualizerSystem : VisualizerSystem<ParticleAcceleratorPartVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ParticleAcceleratorPartVisualizerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, ParticleAcceleratorPartVisualizerComponent comp, ComponentInit args)
    {
        if(!TryComp<SpriteComponent>(uid, out var sprite))
        {
            throw new EntityCreationException("No sprite component found in entity that has ParticleAcceleratorPartVisualizer");
        }

        if(!sprite.AllLayers.Any())
        {
            throw new EntityCreationException("No Layer set for entity that has ParticleAcceleratorPartVisualizer");
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, ParticleAcceleratorPartVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        
        if (!AppearanceSystem.TryGetData(uid, ParticleAcceleratorVisuals.VisualState, out ParticleAcceleratorVisualState state, args.Component))
        {
            state = ParticleAcceleratorVisualState.Unpowered;
        }

        if (state != ParticleAcceleratorVisualState.Unpowered)
        {
            args.Sprite.LayerSetVisible(ParticleAcceleratorVisualLayers.Unlit, true);
            args.Sprite.LayerSetState(ParticleAcceleratorVisualLayers.Unlit, comp.BaseState + ParticleAcceleratorPartVisualizerComponent.StatesSuffixes[state]);
        }
        else
        {
            args.Sprite.LayerSetVisible(ParticleAcceleratorVisualLayers.Unlit, false);
        }
    }
}
