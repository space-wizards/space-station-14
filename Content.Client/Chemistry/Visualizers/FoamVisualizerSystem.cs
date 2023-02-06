using Content.Shared.Foam;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.Visualizers;

/// <summary>
/// The system responsible for ensuring <see cref="FoamVisualsComponent"/> plays the animation it's meant to when the foam dissolves.
/// </summary>
public sealed class FoamVisualizerSystem : VisualizerSystem<FoamVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FoamVisualsComponent, ComponentInit>(OnComponentInit);
    }

    /// <summary>
    /// Generates the animation used by foam visuals when the foam dissolves.
    /// </summary>
    private void OnComponentInit(EntityUid uid, FoamVisualsComponent comp, ComponentInit args)
    {
        comp.Animation = new Animation()
        {
            Length = TimeSpan.FromSeconds(comp.AnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = FoamVisualLayers.Base,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(comp.State, 0f)
                    }
                }
            }
        };
    }

    /// <summary>
    /// Plays the animation used by foam visuals when the foam dissolves.
    /// </summary>
    protected override void OnAppearanceChange(EntityUid uid, FoamVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (AppearanceSystem.TryGetData<bool>(uid, FoamVisuals.State, out var state, args.Component) && state)
        {
            if (TryComp(uid, out AnimationPlayerComponent? animPlayer)
            && !AnimationSystem.HasRunningAnimation(uid, animPlayer, FoamVisualsComponent.AnimationKey))
                AnimationSystem.Play(uid, animPlayer, comp.Animation, FoamVisualsComponent.AnimationKey);
        }

        if (AppearanceSystem.TryGetData<Color>(uid, FoamVisuals.Color, out var color, args.Component))
        {
            if (args.Sprite != null)
                args.Sprite.Color = color;
        }
    }
}

public enum FoamVisualLayers : byte
{
    Base
}
