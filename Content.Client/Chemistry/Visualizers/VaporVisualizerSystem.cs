using Content.Shared.Vapor;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.Visualizers;

/// <summary>
/// Handles vapor playing the 'being sprayed' animation if necessary.
/// </summary>
public sealed class VaporVisualizerSystem : VisualizerSystem<VaporVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VaporVisualsComponent, ComponentInit>(OnComponentInit);
    }

    /// <summary>
    /// Constructs the 'being sprayed' animation for the vapor entity.
    /// </summary>
    private void OnComponentInit(EntityUid uid, VaporVisualsComponent comp, ComponentInit args)
    {
        comp.VaporFlick = new Animation()
        {
            Length = TimeSpan.FromSeconds(comp.AnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = VaporVisualLayers.Base,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(comp.AnimationState, 0f)
                    }
                }
            }
        };
    }

    /// <summary>
    /// Ensures that the vapor entity plays its 'being sprayed' animation if necessary.
    /// </summary>
    protected override void OnAppearanceChange(EntityUid uid, VaporVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (AppearanceSystem.TryGetData<Color>(uid, VaporVisuals.Color, out var color, args.Component) && args.Sprite != null)
        {
            args.Sprite.Color = color;
        }

        if ((AppearanceSystem.TryGetData<bool>(uid, VaporVisuals.State, out var state, args.Component) && state) &&
            TryComp<AnimationPlayerComponent>(uid, out var animPlayer) &&
           !AnimationSystem.HasRunningAnimation(uid, animPlayer, VaporVisualsComponent.AnimationKey))
        {
            AnimationSystem.Play(uid, animPlayer, comp.VaporFlick, VaporVisualsComponent.AnimationKey);
        }
    }
}

public enum VaporVisualLayers : byte
{
    Base
}
