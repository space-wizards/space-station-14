using Content.Shared.CPR;
using Robust.Client.GameObjects;
using Robust.Client.Animations;
using Robust.Shared.Animations;
using System.Numerics;

namespace Content.Client.CPR;

public sealed partial class CPRSystem : SharedCPRSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;

    private const string LungeKey = "cpr-lunge";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CPRLungeEvent>(OnCPRLunge);
    }
    /// <summary>
    /// Used for playing the animation of other entities doing CPR
    /// </summary>
    /// <param name="args">The lunge event</param>
    public void OnCPRLunge(CPRLungeEvent args)
    {
        var ent = GetEntity(args.Ent);
        if (Exists(ent)) DoLunge(ent);
    }

    public override void DoLunge(EntityUid user)
    {
        if (!Timing.IsFirstTimePredicted)
            return;
        // play the CPR animation
        var lunge = GetLungeAnimation(new Vector2(0f, -1f)); // downwards vector, as the animation has to go downwards to mimic the motion of CPR
        _animation.Stop(user, LungeKey);
        _animation.Play(user, lunge, LungeKey);
    }

    private Animation GetLungeAnimation(Vector2 direction)
    {
        const float endLength = CPRAnimationLength;

        var animationTrack = new AnimationTrackComponentProperty()
        {
            ComponentType = typeof(SpriteComponent),
            Property = nameof(SpriteComponent.Offset),
            InterpolationMode = AnimationInterpolationMode.Cubic,
            KeyFrames =
            {
                new AnimationTrackProperty.KeyFrame(new Vector2(0f,0f), 0f),
                new AnimationTrackProperty.KeyFrame(direction.Normalized() * 0.12f, endLength * 0.2f),
                new AnimationTrackProperty.KeyFrame(direction.Normalized() * 0.16f, endLength * 0.4f),
                new AnimationTrackProperty.KeyFrame(new Vector2(0f,0f), endLength)
            }
        };

        return new Animation
        {
            Length = TimeSpan.FromSeconds(CPRAnimationEndTime),
            AnimationTracks =
            {
                animationTrack
            }
        };
    }
}
