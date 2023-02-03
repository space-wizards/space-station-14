using Content.Shared.Foam;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.Visualizers;

public sealed class FoamVisualizerSystem : VisualizerSystem<FoamVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FoamVisualizerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, FoamVisualizerComponent comp, ComponentInit args)
    {
        var flick = new AnimationTrackSpriteFlick();
        flick.LayerKey = FoamVisualLayers.Base;
        flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(comp.State, 0f));
        comp.Animation = new Animation {Length = TimeSpan.FromSeconds(comp.Delay)};
        comp.Animation.AnimationTracks.Add(flick);
    }

    protected override void OnAppearanceChange(EntityUid uid, FoamVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if(!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        if (AppearanceSystem.TryGetData<bool>(uid, FoamVisuals.State, out var state, appearance) && state)
        {
            if (TryComp(uid, out AnimationPlayerComponent? animPlayer)
            && !AnimationSystem.HasRunningAnimation(uid, animPlayer, FoamVisualizerComponent.AnimationKey))
                AnimationSystem.Play(uid, animPlayer, comp.Animation, FoamVisualizerComponent.AnimationKey);
        }

        if (AppearanceSystem.TryGetData<Color>(uid, FoamVisuals.Color, out var color, appearance))
        {
            if (TryComp(uid, out SpriteComponent? sprite))
                sprite.Color = color;
        }
    }
}

public enum FoamVisualLayers : byte
{
    Base
}
