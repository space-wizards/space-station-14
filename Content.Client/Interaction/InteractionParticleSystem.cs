using System.Numerics;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.Interaction;

public sealed partial class InteractionParticleSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private AnimationPlayerSystem _animation = default!;
    [Dependency] private TransformSystem _xform = default!;

    [Dependency] private EntityQuery<DoAfterComponent> _doafterQuery = default!;

    private const string AnimateKey = "particle-animation";

    private static readonly Dictionary<InteractionParticleType, EntProtoId> InteractionParticleIds = new ()
    {
        { InteractionParticleType.Use, "InteractionParticleUse" },
        { InteractionParticleType.Pull, "InteractionParticlePull" },
        { InteractionParticleType.InHand, "InteractionParticleUse" },
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<InteractionParticleEvent>(OnInteractionParticle);
    }

    private void OnInteractionParticle(InteractionParticleEvent ev)
    {
        var performer = GetEntity(ev.Performer);
        var used = GetEntity(ev.Used);
        var target = GetEntity(ev.Target);

        if (!Exists(performer) || !Exists(target))
            return;

        var type = ev.Type;
        if (type == InteractionParticleType.Pull)
        {
            (performer, target) = (target, performer);
        }

        var performerXform = Transform(performer);
        var targetXform = Transform(target);
        if (performerXform.MapID == MapId.Nullspace || targetXform.MapID == MapId.Nullspace)
            return;

        // if the interaction is happening across parent boundaries (ie inhand or in a bag or something)
        // override it with an inhand particle effect
        if (performerXform.ParentUid != targetXform.ParentUid)
        {
            if (type == InteractionParticleType.Pull)
                return;

            type = InteractionParticleType.InHand;
        }

        var performerTargetDelta = targetXform.LocalPosition - performerXform.LocalPosition;
        var particle = Spawn(InteractionParticleIds[type], performerXform.Coordinates);

        var doAfterOffset = 0f;
        if (type == InteractionParticleType.InHand)
        {
            used = target;
            _xform.SetParent(particle, performer);

            if (_doafterQuery.TryComp(performer, out var activeDoAfter))
            {
                var count = activeDoAfter.DoAfters.Count;
                doAfterOffset = count * 0.20f;
            }
        }

        var inHandDelta = new Vector2(0, 0.85f + doAfterOffset);

        if (used is { } usedEntity && Exists(usedEntity) && TryComp<SpriteComponent>(usedEntity, out var usedSprite))
        {
            _sprite.CopySprite((usedEntity, usedSprite), particle);
            _sprite.SetDrawDepth(particle, (int) Shared.DrawDepth.DrawDepth.Effects);
        }

        var sprite = Comp<SpriteComponent>(particle);
        sprite.NoRotation = true;
        var spriteColor = sprite.Color;
        var startPos = performerXform.LocalPosition;
        var animation = type switch
        {
            InteractionParticleType.Use => GetUseAnimation(startPos, startPos + performerTargetDelta, spriteColor),
            InteractionParticleType.Pull => GetPullAnimation(startPos, startPos + performerTargetDelta, spriteColor),
            InteractionParticleType.InHand => GetUseAnimation(Vector2.Zero, inHandDelta, spriteColor, true),
            _ => throw new ArgumentOutOfRangeException(nameof(ev), $"Interaction particle event has unknown particle type {type}"),
        };
        _animation.Play(particle, animation, AnimateKey);
    }

    private Animation GetUseAnimation(Vector2 startPosition, Vector2 endPosition, Color color, bool spriteOffset = false)
    {
        var startRotation = _random.NextAngle(Angle.FromDegrees(-40), Angle.FromDegrees(40));
        var endRotation = Angle.Zero;
        var startScale = new Vector2(0.3f, 0.3f);
        var endScale = new Vector2(1f, 1f);
        var rotationLength = TimeSpan.FromMilliseconds(600);

        var offsetLength = TimeSpan.FromMilliseconds(250);

        var startColor = color.WithAlpha(color.A * 0.7f);
        var endColor = color.WithAlpha(0f);
        var colorLength = rotationLength + offsetLength;

        // use anim lerps transform local position
        // but inhand just lerps sprite offset (since it gets parented to the performer)
        var posTrack = spriteOffset
            ? new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Offset),
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(startPosition, 0f),
                    new AnimationTrackProperty.KeyFrame(endPosition, (float)offsetLength.TotalSeconds, Easings.OutBack),
                },
            }
            : new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(TransformComponent),
                Property = nameof(TransformComponent.LocalPosition),
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(startPosition, 0f),
                    new AnimationTrackProperty.KeyFrame(endPosition, (float)offsetLength.TotalSeconds, Easings.OutBack),
                },
            };

        return new Animation()
        {
            Length = colorLength,

            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(startRotation, 0f),
                        new AnimationTrackProperty.KeyFrame(endRotation, (float)rotationLength.TotalSeconds, Easings.OutBack),
                    },
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(startScale, 0f),
                        new AnimationTrackProperty.KeyFrame(endScale, (float)rotationLength.TotalSeconds, Easings.OutBack),
                    },
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(startColor, 0f),
                        new AnimationTrackProperty.KeyFrame(startColor, (float)rotationLength.TotalSeconds),
                        new AnimationTrackProperty.KeyFrame(endColor, (float)offsetLength.TotalSeconds, Easings.InOutCirc),
                    },
                },
                posTrack
            },
        };
    }

    private Animation GetPullAnimation(Vector2 startPosition, Vector2 endPosition, Color color)
    {
        var rotationLength = TimeSpan.FromMilliseconds(8f * (1000f / 12f));

        var offsetLength = TimeSpan.FromMilliseconds(4f * (1000f / 12f));

        var endColor = color.WithAlpha(0f);

        return new Animation
        {
            Length = rotationLength,

            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(TransformComponent),
                    Property = nameof(TransformComponent.LocalPosition),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(startPosition, 0f),
                        new AnimationTrackProperty.KeyFrame(endPosition, (float)rotationLength.TotalSeconds, Easings.InOutCirc),
                    },
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(color, 0f),
                        new AnimationTrackProperty.KeyFrame(color, (float)offsetLength.TotalSeconds),
                        new AnimationTrackProperty.KeyFrame(endColor, (float)rotationLength.TotalSeconds, Easings.InOutCirc),
                    },
                },
            },
        };
    }
}
