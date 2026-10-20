using System.Numerics;
using Content.Shared.Medical;
using Content.Shared.Medical.Healing;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Timing;
using static Robust.Client.Animations.AnimationTrackProperty;
using Robust.Shared.Random;

namespace Content.Client.Medical.Healing;

/// <inheritdoc/>
public sealed partial class ClientHealingSystem : HealingSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private AnimationPlayerSystem _animations = default!;
    [Dependency] private IRobustRandom _random = default!;

    [SubscribeNetworkEvent]
    private void HandlePickupAnimation(HealingAnimationEvent msg)
    {
        HealAnimation(GetEntity(msg.Target));
    }

    /// <inheritdoc/>
    public override void PlayHealAnimation(EntityUid target, EntityUid? user = null)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        HealAnimation(target);
    }

    /// <summary>
    ///     Animates a target by recoloring it in <param name="color"></param> and spawns <param name="particles">amount</param> of the healing particles
    ///     Probably shouldn't be called directly.
    /// </summary>
    private void HealAnimation(EntityUid target, Color? color = null, int particles = 6)
    {
        color ??= Color.Green;
        if (Deleted(target))
            return;

        var metadata = MetaData(target);

        if (IsPaused(target, metadata))
            return;

        if (!TryComp<SpriteComponent>(target, out var targetSprite))
        {
            Log.Error("Entity ({0}) couldn't be animated for healing since it doesn't have a {1}!", metadata.EntityName, nameof(SpriteComponent));
            return;
        }

        for (var i = 0; i < particles; i++)
        {
            var spriteBounds = _sprite.GetLocalBounds((target, targetSprite));
            var offset = _random.NextVector2Box(spriteBounds.Size.X, spriteBounds.Size.Y) / 2;
            var coords = _xform.ToMapCoordinates(Transform(target).Coordinates).Offset(offset);
            var spark = Spawn("HealingSparkle", coords);
            var animationsSpark = Comp<AnimationPlayerComponent>(spark);
            var curCoords = Transform(spark).LocalPosition;
            var final = new Vector2(0.2f, 0.2f);
            if (!TryComp(spark, out SpriteComponent? sparkSprite))
            {
                Log.Error("Entity ({0}) couldn't be animated for healing since it doesn't have a {1}!", metadata.EntityName, nameof(SpriteComponent));
                return;
            }

            var curScale = sparkSprite.Scale;
            var curRot = sparkSprite.Rotation;
            _animations.Play(new Entity<AnimationPlayerComponent>(spark, animationsSpark),
                new Animation
                {
                    Length = TimeSpan.FromSeconds(1),
                    AnimationTracks =
                    {
                        new AnimationTrackComponentProperty
                        {
                            ComponentType = typeof(SpriteComponent),
                            Property = nameof(SpriteComponent.Scale),
                            InterpolationMode = AnimationInterpolationMode.Linear,
                            KeyFrames =
                            {
                                new KeyFrame(curScale, 0),
                                new KeyFrame(new Vector2(0.1f, 0.1f), 1f),
                            },
                        },
                        new AnimationTrackComponentProperty
                        {
                            ComponentType = typeof(SpriteComponent),
                            Property = nameof(SpriteComponent.Rotation),
                            InterpolationMode = AnimationInterpolationMode.Linear,
                            KeyFrames =
                            {
                                new KeyFrame(curRot, 0),
                                new KeyFrame(curRot + new Angle(90), 1f),
                            },
                        },
                        new AnimationTrackComponentProperty
                        {
                            ComponentType = typeof(TransformComponent),
                            Property = nameof(TransformComponent.LocalPosition),
                            InterpolationMode = AnimationInterpolationMode.Linear,
                            KeyFrames =
                            {
                                new KeyFrame(curCoords, 0),
                                new KeyFrame(curCoords + final, 0.5f),
                                new KeyFrame(curCoords + final, 1f),
                            },
                        },
                    },
                },
                "heal_particles");
        }

        var animations = Comp<AnimationPlayerComponent>(target);
        var curColor = targetSprite.Color;

        _animations.Play(new Entity<AnimationPlayerComponent>(target, animations),
            new Animation
        {
            Length = TimeSpan.FromMilliseconds(300),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new KeyFrame(curColor, 0),
                        new KeyFrame(color, 0.125f),
                        new KeyFrame(curColor, 0.150f),
                        new KeyFrame(curColor, 0.200f),
                        new KeyFrame(curColor, 0.250f),
                        new KeyFrame(curColor, 0.300f),
                    },
                },
            },
        },
            "heal_anim");
    }
}
