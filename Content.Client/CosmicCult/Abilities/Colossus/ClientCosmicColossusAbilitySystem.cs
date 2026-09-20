using System.Numerics;
using Content.Shared.CosmicCult.Abilities.Colossus;
using Content.Shared.CosmicCult.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.CosmicCult.Abilities.Colossus;

public sealed partial class ClientCosmicColossusAbilitySystem : CosmicColossusAbilitySystem
{
    [Dependency] private AnimationPlayerSystem _anim = default!;

    private const string KeySunder = "colossus-sunder";
    private const string KeyHibernate = "colossus-hibernate";

    [SubscribeLocalEvent]
    private void OnColossusAppearance(Entity<CosmicColossusComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!args.AppearanceData.TryGetValue(ColossusVisuals.Visuals, out var obj)
            || obj is not ColossusStatus status
            || status == ent.Comp.LastStatus)
            return;

        ent.Comp.LastStatus = status;

        switch (status)
        {
            case ColossusStatus.Sunder:
                SunderVisuals(ent);
                break;
            case ColossusStatus.Hibernate:
                HibernateVisuals(ent);
                break;
        }
    }

    private void SunderVisuals(Entity<CosmicColossusComponent> ent)
    {
        if (_anim.HasRunningAnimation(ent, KeySunder))
            return;

        var vfxEnt = Spawn(ent.Comp.SunderVfx, Transform(ent).Coordinates);
        _anim.Play(ent, AnimSunderEnt(), KeySunder);
        _anim.Play(vfxEnt, AnimSunderVfx(), KeySunder);
    }

    private void HibernateVisuals(Entity<CosmicColossusComponent> ent)
    {
        if (_anim.HasRunningAnimation(ent, KeyHibernate))
            return;

        Spawn(ent.Comp.CultVfx, Transform(ent).Coordinates);
        _anim.Play(ent, AnimHibernate(), KeyHibernate);
    }

    private static Animation AnimSunderEnt()
    {
        var time = 1.4f;

        return new Animation
        {
            Length = TimeSpan.FromSeconds(time),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Color.White.WithAlpha(0f), 0f),
                        new AnimationTrackProperty.KeyFrame(Color.White.WithAlpha(0f), 1f),
                        new AnimationTrackProperty.KeyFrame(Color.White.WithAlpha(1f), 0.4f),
                    },
                },
            },
        };
    }

    private static Animation AnimSunderVfx()
    {
        var time = 1.4f;

        return new Animation
        {
            Length = TimeSpan.FromSeconds(time),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {

                        new AnimationTrackProperty.KeyFrame(new Vector2(0f, 0f), 0f, Easings.OutBack),
                        new AnimationTrackProperty.KeyFrame(new Vector2(0f, 1.3f), 0.4f, Easings.InOutSine),
                        new AnimationTrackProperty.KeyFrame(new Vector2(0f, 0.55f), 0.05f, Easings.OutBounce),
                        new AnimationTrackProperty.KeyFrame(new Vector2(0f, 0f), 1f),
                    },
                },

                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(new Vector2(0.25f, 0.25f), 0f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(1.35f, 1.35f), 0.2f, Easings.OutElastic),
                        new AnimationTrackProperty.KeyFrame(new Vector2(1f, 1f), 0.1f, Easings.OutElastic),
                        new AnimationTrackProperty.KeyFrame(new Vector2(1.45f, 1.45f), 0.1f, Easings.OutElastic),
                        new AnimationTrackProperty.KeyFrame(new Vector2(1f, 1f), 0.05f),
                    },
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Color.White.WithAlpha(1f), 0f),
                        new AnimationTrackProperty.KeyFrame(Color.White.WithAlpha(1f), 1f),
                        new AnimationTrackProperty.KeyFrame(Color.White.WithAlpha(0f), 0.4f),
                    },
                },
            },
        };
    }

    private static Animation AnimHibernate()
    {
        var animTime = 0.4f;

        return new Animation
        {
            Length = TimeSpan.FromSeconds(animTime),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(new Vector2(1f, 1f), 0f, Easings.OutSine),
                        new AnimationTrackProperty.KeyFrame(new Vector2(0.75f, 0.75f), animTime*0.4f, Easings.InOutCirc),
                        new AnimationTrackProperty.KeyFrame(new Vector2(1f, 1f), animTime*0.6f, Easings.OutBack),
                    },
                },
            },
        };
    }
}
