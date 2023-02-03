using Content.Shared.Vapor;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.Visualizers;
public sealed class VaporVisualizerSystem : VisualizerSystem<VaporVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VaporVisualizerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, VaporVisualizerComponent comp, ComponentInit args)
    {
        comp.VaporFlick = new Animation {Length = TimeSpan.FromSeconds(comp.Delay)};
        {
            var flick = new AnimationTrackSpriteFlick();
            comp.VaporFlick.AnimationTracks.Add(flick);
            flick.LayerKey = VaporVisualLayers.Base;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(comp.State, 0f));
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, VaporVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if(!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        if(AppearanceSystem.TryGetData<Color>(uid, VaporVisuals.Color, out var color, appearance))
        {
            if (TryComp<SpriteComponent>(uid, out var sprite))
                sprite.Color = color;
        }
        
        if(AppearanceSystem.TryGetData<bool>(uid, VaporVisuals.State, out var state, appearance) && state)
        {
            if (TryComp<AnimationPlayerComponent>(uid, out var animPlayer))
            {
                if(!AnimationSystem.HasRunningAnimation(uid, animPlayer, VaporVisualizerComponent.AnimationKey))
                    AnimationSystem.Play(uid, animPlayer, comp.VaporFlick, VaporVisualizerComponent.AnimationKey);
            }
        }
    }
}

public enum VaporVisualLayers : byte
{
    Base
}
