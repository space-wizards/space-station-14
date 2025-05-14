// these are HEAVILY based on the Bingle free-agent ghostrole from GoobStation, but reflavored and reprogrammed to make them more Robust (and less of a meme.)
// all credit for the core gameplay concepts and a lot of the core functionality of the code goes to the folks over at Goob, but I re-wrote enough of it to justify putting it in our filestructure.
// the original Bingle PR can be found here: https://github.com/Goob-Station/Goob-Station/pull/1519

using Content.Shared._Impstation.Replicator;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client._Impstation.Replicator;

/// <summary>
///     Handles the falling animation for entities that fall into a Binglepit. shamlesly copied from chasm
///     imp note: i didn't really change much here, aside from updating it to fit current Entity<T> conventions. 
/// </summary>
public sealed class ReplicatorNestFallingVisualsSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _anim = default!;

    private readonly string _holeFallingAnimationKey = "hole_fall";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplicatorNestFallingComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ReplicatorNestFallingComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentInit(Entity<ReplicatorNestFallingComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite) || TerminatingOrDeleted(ent))
            return;

        ent.Comp.OriginalScale = sprite.Scale;

        var animPlayer = EnsureComp<AnimationPlayerComponent>(ent);
        if (_anim.HasRunningAnimation(animPlayer, _holeFallingAnimationKey))
            return;

        _anim.Play((ent, animPlayer), GetFallingAnimation(ent.Comp), _holeFallingAnimationKey);
    }

    private void OnComponentRemove(Entity<ReplicatorNestFallingComponent> ent, ref ComponentRemove args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite) || TerminatingOrDeleted(ent))
            return;

        var animPlayer = EnsureComp<AnimationPlayerComponent>(ent);
        var animEnt = (Entity<AnimationPlayerComponent?>)(ent, animPlayer);

        if (_anim.HasRunningAnimation(animPlayer, _holeFallingAnimationKey))
            _anim.Stop(animEnt, _holeFallingAnimationKey);

        sprite.Scale = ent.Comp.OriginalScale;
    }

    private static Animation GetFallingAnimation(ReplicatorNestFallingComponent component)
    {
        var length = component.AnimationTime;

        return new Animation()
        {
            Length = length,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(component.OriginalScale, 0.0f),
                        new AnimationTrackProperty.KeyFrame(component.AnimationScale, length.Seconds),
                    },
                    InterpolationMode = AnimationInterpolationMode.Cubic
                }
            }
        };
    }
}
