using System.Numerics;
using Robust.Client.Animations;
using Robust.Client.Animus.Actions;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.Animus.Actions;

public sealed partial class AnimusActionAnimationHopping : AnimusActionAnimationBase
{
    ///<summary>
    /// How high should they hop? Higher hop = more energy.
    /// </summary>
    [DataField]
    public float HopIntensity = 0.35f;

    /// <summary>
    /// How long should the hop take?
    /// </summary>
    [DataField]
    public float AnimationLength = 0.3f;

    protected override Animation GetNextAnimation(AppearanceSystem appearanceSystem, EntityUid entity, bool restarting)
    {
        var anim = new Animation()
        {
            Length = TimeSpan.FromSeconds(AnimationLength),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(new Vector2(), 0),
                        new AnimationTrackProperty.KeyFrame(new Vector2(0, HopIntensity), AnimationLength / 3),
                        new AnimationTrackProperty.KeyFrame(new Vector2(), AnimationLength / 3 * 2),
                    },
                },
            },
        };
        return anim;
    }

    protected override Animation? GetStopAnimation(AppearanceSystem appearanceSystem, EntityUid entity)
    {
        return StopAnimation;
    }
}
