using Robust.Shared.GameStates;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Shared.Serialization;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Client.GameObjects;
using Robust.Client.Animations;
using System.Numerics;
using Robust.Shared.Animations;
using Content.Client.DoAfter;
using Content.Shared.CPR;
using Robust.Shared.Timing;

namespace Content.Client.CPR;

public sealed partial class ClientCPRSystem : SharedCPRSystem
{
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    private const string LungeKey = "cpr-lunge";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<CPRLungeEvent>(OnLunge);
    }
    /// <summary>
    /// Used for playing the animation of other entities doing CPR
    /// </summary>
    /// <param name="evn">The lunge event</param>
    public void OnLunge(CPRLungeEvent evn)
    {
        var ent = GetEntity(evn.Ent);
        if (Exists(ent)) DoLunge(ent);
    }

    public override void DoLunge(EntityUid user)
    {
        if (!Timing.IsFirstTimePredicted)
            return;
        // play the CPR animation
        var lunge = GetLungeAnimation(new Vector2(0f, -1f));
        _animation.Stop(user, LungeKey);
        _animation.Play(user, lunge, LungeKey);
    }

    private Animation GetLungeAnimation(Vector2 direction)
    {
        const float endLength = 0.2f;

        return new Animation
        {
            Length = TimeSpan.FromSeconds(endLength * 5f), // has to be long, otherwise cuts off in the middle for some reason
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
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
                }
            }
        };
    }
}
