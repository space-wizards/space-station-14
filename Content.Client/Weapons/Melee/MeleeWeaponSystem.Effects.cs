using System.Numerics;
using Content.Client.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee;
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
    public override void DoLunge(EntityUid user, EntityUid weapon, Angle angle, Vector2 localPos, string? animation, bool predicted = true)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        var lunge = GetLungeAnimation(localPos);

        // Stop any existing lunges on the user.
        _animation.Stop(user, MeleeLungeKey);
        _animation.Play(user, lunge, MeleeLungeKey);

        if (localPos == Vector2.Zero || animation == null)
            return;

        if (!_xformQuery.TryGetComponent(user, out var userXform) || userXform.MapID == MapId.Nullspace)
            return;

        var animationUid = Spawn(animation, userXform.Coordinates);

        if (!TryComp<SpriteComponent>(animationUid, out var sprite)
            || !TryComp<WeaponArcVisualsComponent>(animationUid, out var arcComponent))
        {
            return;
        }

        var length = 1f; //CrystallPunk Melee upgrade
        var offset = -1f; //CrystallPunk Melee upgrade

        var spriteRotation = Angle.Zero;
        if (arcComponent.Animation != WeaponArcAnimation.None
            && TryComp(weapon, out MeleeWeaponComponent? meleeWeaponComponent))
        {
            if (user != weapon
                && TryComp(weapon, out SpriteComponent? weaponSpriteComponent))
                sprite.CopyFrom(weaponSpriteComponent);

            spriteRotation = meleeWeaponComponent.WideAnimationRotation;

            if (meleeWeaponComponent.SwingLeft)
                angle *= -1;

            length = meleeWeaponComponent.CPAnimationLength; //CrystallPunk Melee upgrade
            offset = meleeWeaponComponent.CPAnimationOffset; //CrystallPunk Melee upgrade
        }
        sprite.NoRotation = true;
        sprite.Rotation = localPos.ToWorldAngle();
        var distance = Math.Clamp(localPos.Length() / 2f, 0.2f, 1f);

        var xform = _xformQuery.GetComponent(animationUid);

        switch (arcComponent.Animation)
        {
            case WeaponArcAnimation.Slash:
                _animation.Play(animationUid, GetSlashAnimation(sprite, angle, spriteRotation), SlashAnimationKey);
                TransformSystem.SetParent(animationUid, xform, user, userXform);
                if (arcComponent.Fadeout)
                    _animation.Play(animationUid, GetFadeAnimation(sprite, 0.065f, 0.065f + 0.05f), FadeAnimationKey);
                break;
            case WeaponArcAnimation.Thrust:
                _animation.Play(animationUid, GetThrustAnimation(sprite, distance, spriteRotation), ThrustAnimationKey);
                TransformSystem.SetParent(animationUid, xform, user, userXform);
                if (arcComponent.Fadeout)
                    _animation.Play(animationUid, GetFadeAnimation(sprite, 0.05f, 0.15f), FadeAnimationKey);
                break;
            case WeaponArcAnimation.None:
                var (mapPos, mapRot) = TransformSystem.GetWorldPositionRotation(userXform);
                TransformSystem.AttachToGridOrMap(animationUid, xform);
                var worldPos = mapPos + (mapRot - userXform.LocalRotation).RotateVec(localPos);
                var newLocalPos = TransformSystem.GetInvWorldMatrix(xform.ParentUid).Transform(worldPos);
                TransformSystem.SetLocalPositionNoLerp(xform, newLocalPos);
                if (arcComponent.Fadeout)
                    _animation.Play(animationUid, GetFadeAnimation(sprite, 0f, 0.15f), FadeAnimationKey);
                break;
            //CrystallPunk MeleeUpgrade
            case WeaponArcAnimation.CPSlash:
                _animation.Play(animationUid, CPGetSlashAnimation(sprite, angle, spriteRotation, length, offset), SlashAnimationKey);
                TransformSystem.SetParent(animationUid, xform, user, userXform);
                if (arcComponent.Fadeout)
                    _animation.Play(animationUid, GetFadeAnimation(sprite, length * 0.5f, length + 0.15f), FadeAnimationKey);
                break;
            case WeaponArcAnimation.CPThrust:
                _animation.Play(animationUid, CPGetThrustAnimation(sprite, -offset, spriteRotation, length), ThrustAnimationKey);
                TransformSystem.SetParent(animationUid, xform, user, userXform);
                if (arcComponent.Fadeout)
                    _animation.Play(animationUid, GetFadeAnimation(sprite, 0f, 0.15f), FadeAnimationKey);
                break;
            //CrystallPunk MeleeUpgrade end
        }
    }

    private Animation GetSlashAnimation(SpriteComponent sprite, Angle arc, Angle spriteRotation)
    {
        const float slashStart = 0.03f;
        const float slashEnd = 0.065f;
        const float length = slashEnd + 0.05f;
        var startRotation = sprite.Rotation + arc / 2;
        var endRotation = sprite.Rotation - arc / 2;
        var startRotationOffset = startRotation.RotateVec(new Vector2(0f, -1f));
        var endRotationOffset = endRotation.RotateVec(new Vector2(0f, -1f));
        startRotation += spriteRotation;
        endRotation += spriteRotation;
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
                        new AnimationTrackProperty.KeyFrame(startRotationOffset, 0f),
                        new AnimationTrackProperty.KeyFrame(startRotationOffset, slashStart),
                        new AnimationTrackProperty.KeyFrame(endRotationOffset, slashEnd)
                    }
                },
            }
        };
    }

    private Animation GetThrustAnimation(SpriteComponent sprite, float distance, Angle spriteRotation)
    {
        const float thrustEnd = 0.05f;
        const float length = 0.15f;
        var startOffset = sprite.Rotation.RotateVec(new Vector2(0f, -distance / 5f));
        var endOffset = sprite.Rotation.RotateVec(new Vector2(0f, -distance));

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
                        new AnimationTrackProperty.KeyFrame(startOffset, 0f),
                        new AnimationTrackProperty.KeyFrame(endOffset, thrustEnd),
                        new AnimationTrackProperty.KeyFrame(endOffset, length),
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
        const float length = 0.2f; // 0.1 original, CrystallPunk update

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
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0f), //CrystallPunk MeleeUpgrade 
                        new AnimationTrackProperty.KeyFrame(direction.Normalized() * 0.15f, length*0.4f), //CrystallPunk MeleeUpgrade
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, length*0.8f) //CrystallPunk MeleeUpgrade
                    }
                }
            }
        };
    }

    //CrystallPunk MeleeUpgrade start
    private Animation CPGetSlashAnimation(SpriteComponent sprite, Angle arc, Angle spriteRotation, float length, float offset = -1f)
    {
        var startRotation = sprite.Rotation + (arc * 0.5f);
        var endRotation = sprite.Rotation - (arc * 0.5f);

        var startRotationOffset = startRotation.RotateVec(new Vector2(0f, offset));
        var endRotationOffset = endRotation.RotateVec(new Vector2(0f, offset));

        startRotation += spriteRotation;
        endRotation += spriteRotation;
        sprite.NoRotation = true;

        return new Animation()
        {
            Length = TimeSpan.FromSeconds(length + 0.05f),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Angle.Lerp(startRotation,endRotation,0.0f), length * 0.0f),
                        new AnimationTrackProperty.KeyFrame(Angle.Lerp(startRotation,endRotation,1.0f), length * 0.6f),
                        new AnimationTrackProperty.KeyFrame(Angle.Lerp(startRotation,endRotation,0.8f), length * 1.0f),
                    }
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Vector2.Lerp(startRotationOffset,endRotationOffset,0.0f), length * 0.0f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Lerp(startRotationOffset,endRotationOffset,1.0f), length * 0.6f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Lerp(startRotationOffset,endRotationOffset,0.8f), length * 1.0f),
                    }
                },
            }
        };
    }

    private Animation CPGetThrustAnimation(SpriteComponent sprite, float distance, Angle spriteRotation, float length)
    {
        var startOffset = sprite.Rotation.RotateVec(new Vector2(0f, -distance / 5f));
        var endOffset = sprite.Rotation.RotateVec(new Vector2(0f, -distance));

        sprite.Rotation += spriteRotation;

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
                        new AnimationTrackProperty.KeyFrame(Vector2.Lerp(startOffset, endOffset, 0f), length * 0f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Lerp(startOffset, endOffset, 1f), length * 0.5f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Lerp(startOffset, endOffset, 0.9f), length * 0.8f),
                    }
                },
            }
        };
    }

    //CrystallPunk MeleeUpgrade end

}
