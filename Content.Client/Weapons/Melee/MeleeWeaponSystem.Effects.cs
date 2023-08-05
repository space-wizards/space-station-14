using System.Numerics;
using Content.Client.Weapons.Melee.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Map;

namespace Content.Client.Weapons.Melee;

public sealed partial class MeleeWeaponSystem
{
    private const string FadeAnimationKey = "melee-fade";
    private const string SlashAnimationKey = "melee-slash";
    private const string ThrustAnimationKey = "melee-thrust";

    /// <summary>
    /// Does all of the melee effects for a player that are predicted, i.e. character lunge and weapon animation.
    /// </summary>
    public override void DoLunge(EntityUid user, Angle angle, Vector2 localPos, string? animation, bool predicted = true)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        var lunge = GetLungeAnimation(localPos);

        // Stop any existing lunges on the user.
        _animation.Stop(user, MeleeLungeKey);
        _animation.Play(user, lunge, MeleeLungeKey);

        if (localPos == Vector2.Zero || animation == null)
            return;

        if (!TryComp<TransformComponent>(user, out var userXform) || userXform.MapID == MapId.Nullspace)
            return;

        var animationUid = Spawn(animation, userXform.Coordinates);

        if (!TryComp<SpriteComponent>(animationUid, out var sprite)
            || !TryComp<WeaponArcVisualsComponent>(animationUid, out var arcComponent))
            return;

        sprite.NoRotation = true;
        sprite.Rotation = localPos.ToWorldAngle();
        var distance = Math.Clamp(localPos.Length() / 2f, 0.2f, 1f);

        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(animationUid);

        switch (arcComponent.Animation)
        {
            case WeaponArcAnimation.Slash:
                _animation.Play(animationUid, GetSlashAnimation(sprite, angle), SlashAnimationKey);
                TransformSystem.SetParent(animationUid, xform, user, userXform);
                if (arcComponent.Fadeout)
                    _animation.Play(animationUid, GetFadeAnimation(sprite, 0.065f, 0.065f + 0.05f), FadeAnimationKey);
                break;
            case WeaponArcAnimation.Thrust:
                _animation.Play(animationUid, GetThrustAnimation(sprite, distance), ThrustAnimationKey);
                TransformSystem.SetParent(animationUid, xform, user, userXform);
                if (arcComponent.Fadeout)
                    _animation.Play(animationUid, GetFadeAnimation(sprite, 0.05f, 0.15f), FadeAnimationKey);
                break;
            case WeaponArcAnimation.None:
                var (mapPos, mapRot) = TransformSystem.GetWorldPositionRotation(userXform, xformQuery);
                TransformSystem.AttachToGridOrMap(animationUid, xform);
                var worldPos = mapPos + (mapRot - userXform.LocalRotation).RotateVec(localPos);
                var newLocalPos = TransformSystem.GetInvWorldMatrix(xform.ParentUid, xformQuery).Transform(worldPos);
                TransformSystem.SetLocalPositionNoLerp(xform, newLocalPos);
                if (arcComponent.Fadeout)
                    _animation.Play(animationUid, GetFadeAnimation(sprite, 0f, 0.15f), FadeAnimationKey);
                break;
        }
    }

    private Animation GetSlashAnimation(SpriteComponent sprite, Angle arc)
    {
        const float slashStart = 0.03f;
        const float slashEnd = 0.065f;
        const float length = slashEnd + 0.05f;
        var startRotation = sprite.Rotation - arc / 2;
        var endRotation = sprite.Rotation + arc / 2;
        sprite.NoRotation = true;

        return new Animation()
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(startRotation, 0f),
                        new AnimationTrackProperty.KeyFrame(startRotation, slashStart),
                        new AnimationTrackProperty.KeyFrame(endRotation, slashEnd)
                    }
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(startRotation.RotateVec(new Vector2(0f, -1f)), 0f),
                        new AnimationTrackProperty.KeyFrame(startRotation.RotateVec(new Vector2(0f, -1f)), slashStart),
                        new AnimationTrackProperty.KeyFrame(endRotation.RotateVec(new Vector2(0f, -1f)), slashEnd)
                    }
                },
            }
        };
    }

    private Animation GetThrustAnimation(SpriteComponent sprite, float distance)
    {
        const float thrustEnd = 0.05f;
        const float length = 0.15f;

        return new Animation()
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Rotation.RotateVec(new Vector2(0f, -distance / 5f)), 0f),
                        new AnimationTrackProperty.KeyFrame(sprite.Rotation.RotateVec(new Vector2(0f, -distance)), thrustEnd),
                        new AnimationTrackProperty.KeyFrame(sprite.Rotation.RotateVec(new Vector2(0f, -distance)), length),
                    }
                },
            }
        };
    }

    private Animation GetFadeAnimation(SpriteComponent sprite, float start, float end)
    {
        return new Animation
        {
            Length = TimeSpan.FromSeconds(end),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Color, start),
                        new AnimationTrackProperty.KeyFrame(sprite.Color.WithAlpha(0f), end)
                    }
                }
            }
        };
    }

    /// <summary>
    /// Get the sprite offset animation to use for mob lunges.
    /// </summary>
    private Animation GetLungeAnimation(Vector2 direction)
    {
        const float length = 0.1f;

        return new Animation
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(direction.Normalized() * 0.15f, 0f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, length)
                    }
                }
            }
        };
    }
}
