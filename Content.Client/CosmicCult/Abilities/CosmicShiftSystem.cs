using System.Numerics;
using Content.Shared.CosmicCult.Abilities;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Animations;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.CosmicCult.Abilities;

public sealed partial class CosmicShiftSystem : SharedCosmicShiftSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;

    private static readonly ProtoId<ShaderPrototype> HorizontalCut = "StellarSpriteCutAnimated";
    private static readonly EntProtoId VfxEntity = "CosmicShiftAbilityVfx";

    [EventSubscription]
    private void OnShiftAnim(CosmicShiftAnimEvent args)
    {
        SetShader(GetEntity(args.Target), args.State);
    }

    private void SetShader(Entity<SpriteComponent?> ent, CosmicShiftState state)
    {
        if (!_spriteQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return;

        var shader = _proto.Index(HorizontalCut).Instance().Duplicate();
        var animTime = (float) _timing.RealTime.TotalSeconds;
        var animDuration = 2f;
        shader.SetParameter("animTime", animTime);
        shader.SetParameter("animDuration", animDuration);
        shader.SetParameter("reverse", false);

        switch (state)
        {
            case CosmicShiftState.In:
                ent.Comp.PostShader = shader;

                var shiftInAnim = new Animation()
                {
                    Length = TimeSpan.FromSeconds(animDuration),
                    AnimationTracks =
                    {
                        new AnimationTrackComponentProperty()
                        {
                            ComponentType = typeof(SpriteComponent),
                            Property = nameof(SpriteComponent.Offset),
                            InterpolationMode = AnimationInterpolationMode.Linear,
                            KeyFrames =
                            {
                                new AnimationTrackProperty.KeyFrame(new Vector2(0f, 0f), 0),
                                new AnimationTrackProperty.KeyFrame(new Vector2(0f, -1f), animDuration, Easings.InCubic),
                            },
                        },
                    },
                };
                _animPlayer.Stop(ent.Owner, "cosmic-shift");
                _animPlayer.Play(ent, shiftInAnim, "cosmic-shift");
                Spawn(VfxEntity, Transform(ent).Coordinates);
                break;
            case CosmicShiftState.Out:
                ent.Comp.PostShader = shader;

                var shiftOutAnim = new Animation()
                {
                    Length = TimeSpan.FromSeconds(animDuration),
                    AnimationTracks =
                    {
                        new AnimationTrackComponentProperty()
                        {
                            ComponentType = typeof(SpriteComponent),
                            Property = nameof(SpriteComponent.Offset),
                            InterpolationMode = AnimationInterpolationMode.Linear,
                            KeyFrames =
                            {
                                new AnimationTrackProperty.KeyFrame(new Vector2(0f, -1f), 0),
                                new AnimationTrackProperty.KeyFrame(new Vector2(0f, 0f), animDuration, Easings.OutCubic),
                            },
                        },
                    },
                };
                shader.SetParameter("reverse", true);
                _animPlayer.Stop(ent.Owner, "cosmic-shift");
                _animPlayer.Play(ent.Owner, shiftOutAnim, "cosmic-shift");
                Spawn(VfxEntity, Transform(ent).Coordinates);
                break;
            case CosmicShiftState.Cancel:
                ent.Comp.PostShader = null;
                _animPlayer.Stop(ent.Owner, "cosmic-shift");
                _sprite.SetOffset(ent.Owner, Vector2.Zero);
                break;
        }
    }
}
