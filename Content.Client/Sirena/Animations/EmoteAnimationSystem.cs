using System.Numerics;
using Content.Shared.Sirena.Animations;
using Robust.Client.Animations;
using Robust.Shared.Animations;
using Robust.Shared.GameStates;
using Robust.Client.GameObjects;
using static Content.Shared.Sirena.Animations.EmoteAnimationComponent;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using System.ComponentModel;

namespace Content.Client.Sirena.Animations;

public class EmoteAnimationSystem : SharedEmoteAnimationSystem
{
    [Dependency] private readonly AnimationPlayerSystem AnimationSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<EmoteAnimationComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, EmoteAnimationComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not EmoteAnimationComponentState state)
            return;

        component.AnimationId = state.AnimationId;

        switch (component.AnimationId)
        {
            case EmoteFlipActionPrototype:
                PlayEmoteFlip(uid);
                break;
            case EmoteJumpActionPrototype:
                PlayEmoteJump(uid);
                break;
            case EmoteTurnActionPrototype:
                PlayEmoteTurn(uid);
                break;
            case EmoteStopTailActionPrototype:
                PlayEmoteStopTail(uid);
                break;
            case EmoteStartTailActionPrototype:
                PlayEmoteStartTail(uid);
                break;
            default:
                break;
        }
    }

    public void PlayEmoteFlip(EntityUid uid)
    {
        var animationKey = "emoteAnimationKeyId";

        if (AnimationSystem.HasRunningAnimation(uid, animationKey))
            return;

        var baseAngle = Angle.Zero;
        if (EntityManager.TryGetComponent(uid, out SpriteComponent? sprite))
        {
            if (sprite != null)
                baseAngle = sprite.Rotation;
        }

        var animation = new Animation
        {
            Length = TimeSpan.FromMilliseconds(500),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(baseAngle.Degrees), 0f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(baseAngle.Degrees + 180), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(baseAngle.Degrees + 360), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(baseAngle.Degrees), 0f),
                    }
                }
            }
        };

        AnimationSystem.Play(uid, animation, animationKey);
    }

    public void PlayEmoteJump(EntityUid uid)
    {
        var animationKey = "emoteAnimationKeyId";

        if (AnimationSystem.HasRunningAnimation(uid, animationKey))
            return;

        var animation = new Animation
        {
            Length = TimeSpan.FromMilliseconds(250),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(0, 1), 0.125f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0.125f),
                    }
                }
            }
        };

        AnimationSystem.Play(uid, animation, animationKey);
    }

    public void PlayEmoteTurn(EntityUid uid)
    {
        var animationKey = "emoteAnimationKeyId_rotate"; // it needs for only rotate anim

        if (AnimationSystem.HasRunningAnimation(uid, animationKey))
            return;

        var animation = new Animation
        {
            Length = TimeSpan.FromMilliseconds(600),  // Пока пусть на 0.6 секунд. В идеале бы до 0.9 на 3 поворота
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(TransformComponent),
                    Property = nameof(TransformComponent.LocalRotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(0), 0f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(90), 0.075f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(180), 0.075f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(270), 0.075f),
                        new AnimationTrackProperty.KeyFrame(Angle.Zero, 0.075f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(90), 0.075f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(180), 0.075f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(270), 0.075f),
                        new AnimationTrackProperty.KeyFrame(Angle.Zero, 0.075f),
                    }
                }
            }
        };

        AnimationSystem.Play(uid, animation, animationKey);
    }

    //Останавливает хвост. Сущность должна иметь имя слоя, содержащее "tail" для остановки анимации.
    public void PlayEmoteStopTail(EntityUid uid)
    {
        var animationKey = "emoteAnimationKeyId";

        if (AnimationSystem.HasRunningAnimation(uid, animationKey))
            return;
        if (!TryComp<SpriteComponent>(uid, out var sprite))
        { return; }

        //sprite.NetSyncEnabled = true;
        foreach (var item in sprite.AllLayers)
        {
            if (item.RsiState.Name != null)
                if (item.RsiState.Name.ToLower().Contains("tail"))
                {
                    item.AutoAnimated = false;
                    item.AnimationTime = 0;
                }
        }

        //foreach (var component in _entityManager.GetComponents(uid))
        //{
        //    _entityManager.Dirty((Robust.Shared.GameObjects.Component)component);
        //}
    }
    public void PlayEmoteStartTail(EntityUid uid)
    {
        var animationKey = "emoteAnimationKeyId";

        if (AnimationSystem.HasRunningAnimation(uid, animationKey))
            return;
        if (!TryComp<SpriteComponent>(uid, out var sprite))
        { return; }

        //sprite.NetSyncEnabled = true;
        foreach (var item in sprite.AllLayers)
        {
            if (item.RsiState.Name != null)
                if (item.RsiState.Name.ToLower().Contains("tail"))
                {
                    item.AnimationTime = 0;
                    item.AutoAnimated = true;
                }
        }

        //foreach (var component in _entityManager.GetComponents(uid))
        //{
        //    _entityManager.Dirty((Robust.Shared.GameObjects.Component) component);
        //}
    }

}
