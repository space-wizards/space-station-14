using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using System.Numerics;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Timer = Robust.Shared.Timing.Timer;
using Robust.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Client.Animations;
using Robust.Shared.Animations;
using Robust.Shared.Physics.Events;

namespace Content.Client.CosmicCult;

public sealed partial class CosmicCultSystem : SharedCosmicCultSystem
{
    [Dependency] private AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private PointLightSystem _light = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private readonly SoundSpecifier _siphonSfx = new SoundPathSpecifier("/Audio/_ST/CosmicCult/Abilities/ability-siphon.ogg");
    private readonly ResPath _rsiPath = new("/Textures/_ST/CosmicCult/Effects/ability-siphon.rsi");

    protected override void OnMonumentInteracted(Entity<CosmicMonumentComponent> ent, ref InteractHandEvent args)
    {
        if (!TryComp<CosmicCultistComponent>(args.User, out var cultComp))
            return;

        if (Timing.IsFirstTimePredicted && cultComp.MonumentVisits <= 0)
            PopUp.PopupEntity(Loc.GetString("cosmiccult-influences-unavailable"), ent, PopupType.Medium);

        if (Timing.IsFirstTimePredicted && cultComp.UnlockedInfluences.Count <= 0)
            PopUp.PopupEntity(Loc.GetString("cosmiccult-influences-maxed"), ent, PopupType.Medium);
        base.OnMonumentInteracted(ent, ref args);
    }

    [SubscribeLocalEvent]
    private void OnCultistMove(Entity<CosmicStarMarkComponent> ent, ref MoveEvent args)
    {
        if (!_animPlayer.HasRunningAnimation(ent, ent.Comp.AnimationKey))
            FloatCultist(ent, ent.Comp.Offset, ent.Comp.AnimationKey, ent.Comp.AnimationTime);
    }

    #region Floating Animation
    private void FloatCultist(EntityUid uid, Vector2 offset, string animationKey, float animationTime)
    {
        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(animationTime),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0f),
                        new AnimationTrackProperty.KeyFrame(offset, animationTime / 2, Easings.InOutSine),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, animationTime / 2, Easings.InOutSine),
                    }
                }
            }
        };
        if (!_animPlayer.HasRunningAnimation(uid, animationKey))
            _animPlayer.Play(uid, animation, animationKey);
    }

    [SubscribeLocalEvent]
    private void OnAnimationCompleted(EntityUid uid, CosmicStarMarkComponent component, AnimationCompletedEvent args)
    {
        if (args.Key != component.AnimationKey)
            return;

        FloatCultist(uid, component.Offset, component.AnimationKey, component.AnimationTime);
    }
    #endregion

    #region Influence Animation
    [SubscribeNetworkEvent]
    private void OnInfluenceGain(InfluenceVisualsEvent args)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        var monument = GetEntity(args.Monument);
        var cultist = GetEntity(args.Target);

        if (PlayerManager.LocalEntity != cultist) // Only play the audiovisual sauce on the user's client and nobody else's.
            return;

        var monumentPos = TransformSystem.GetMapCoordinates(monument).Offset(0, 1.65f);
        var cultistPos = TransformSystem.GetMapCoordinates(cultist).Position;
        var dist = cultistPos - monumentPos.Position;
        var iconEnt = Spawn("InfluenceEffectIcon", Transform(cultist).Coordinates.Offset(new Vector2(0, 0.95f)));
        var effectEnt = Spawn("InfluenceEffectProjectile", TransformSystem.GetMapCoordinates(monument).Offset(0, 1.55f));
        Spawn("InfluenceEffectMonument", TransformSystem.GetMapCoordinates(monument));

        _sprite.LayerSetSprite(iconEnt, 0, args.Icon);
        _sprite.SetRotation(effectEnt, -dist.ToAngle());

        Audio.PlayGlobal(args.GachaSound, cultist, AudioParams.Default.WithVariation(0.025f)); // Play it globally but on the client because we want stereo audio.
        TransformSystem.SetWorldRotationNoLerp(effectEnt, dist.ToAngle());

        var animTime = 4f;
        var influenceAnim = new Animation()
        {
            Length = TimeSpan.FromSeconds(animTime),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(new Vector2(0f, 0f), 0.5f, Easings.OutQuad),
                        new AnimationTrackProperty.KeyFrame(new Vector2(dist.Length(), 0f), 1f, Easings.InQuart),
                    },
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(-dist.ToAngle(), 1f, Easings.OutCirc),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(90), 0.5f, Easings.InCirc),
                    },
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(new Vector2(0.1f, 0.1f), 0),
                        new AnimationTrackProperty.KeyFrame(new Vector2(1f, 1f), 1f, Easings.OutExpo),
                        new AnimationTrackProperty.KeyFrame(new Vector2(0.5f, 0.5f), 0.45f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(1.25f, 1.25f), 0.5f, Easings.OutSine),
                    },
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Color.White, 1.5f),
                        new AnimationTrackProperty.KeyFrame(Color.White.WithAlpha(0f), 0.5f, Easings.OutSine),
                    },
                },
            },
        };
        var influenceIconAnim = new Animation()
        {
            Length = TimeSpan.FromSeconds(animTime),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(new Vector2(0f, 0f), 2f, Easings.InOutQuad),
                        new AnimationTrackProperty.KeyFrame(new Vector2(0f, 0.5f), 2f, Easings.InOutQuad),
                    },
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(new Vector2(0.1f, 0.1f), 0.65f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(1f, 1f), 1.55f, Easings.InOutElastic),
                    },
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Color.White.WithAlpha(0f), 0f, Easings.OutSine),
                        new AnimationTrackProperty.KeyFrame(Color.White.WithAlpha(0f), 1.5f, Easings.OutSine),
                        new AnimationTrackProperty.KeyFrame(Color.White, 0.75f),
                        new AnimationTrackProperty.KeyFrame(Color.White.WithAlpha(0f), 0.75f, Easings.OutSine),
                    },
                },
            },
        };
        _animPlayer.Stop(effectEnt, "influence-vfx");
        _animPlayer.Stop(iconEnt, "influence-icon");
        _animPlayer.Play(effectEnt, influenceAnim, "influence-vfx");
        _animPlayer.Play(iconEnt, influenceIconAnim, "influence-icon");
    }

    #endregion

    #region Malign Font
    [SubscribeLocalEvent]
    private void OnFontApproached(Entity<CosmicFontComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId == "fontCollider" && Timing.IsFirstTimePredicted && HasComp<MobStateComponent>(args.OtherEntity) && !ent.Comp.Activated)
            PlayFontAnimation(ent, ent.Comp.InState, ent.Comp.AnimationKey);
    }

    [SubscribeLocalEvent]
    private void OnFontUnapproached(Entity<CosmicFontComponent> ent, ref EndCollideEvent args)
    {
        if (args.OurFixtureId == "fontCollider" && Timing.IsFirstTimePredicted && HasComp<MobStateComponent>(args.OtherEntity) && !ent.Comp.Activated)
            PlayFontAnimation(ent, ent.Comp.OutState, ent.Comp.AnimationKey, true);
    }

    private void PlayFontAnimation(EntityUid uid, string stateId, string animationKey, bool ending = false)
    {
        if (!TryComp<AnimationPlayerComponent>(uid, out var animation) || !TryComp<SpriteComponent>(uid, out var sprite))
            return;
        var ent = (uid, animation);

        if (_animPlayer.HasRunningAnimation(animation, animationKey))
        {
            _animPlayer.Stop(ent, animationKey);
        }

        if (sprite.BaseRSI == null || !sprite.BaseRSI.TryGetState(stateId, out var state))
            return;
        var animLength = state.AnimationLength;

        var anim = new Animation
        {
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = CosmicFontVisualLayers.Base,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(state.StateId, 0f),
                    },
                },
            },
            Length = TimeSpan.FromSeconds(animLength),
        };

        var lightStart = ending ? 6f : 0f;
        var lightFinish = ending ? 0f : 6f;
        anim.AnimationTracks.Add(new AnimationTrackComponentProperty
        {
            ComponentType = typeof(PointLightComponent),
            Property = nameof(PointLightComponent.Energy),
            InterpolationMode = AnimationInterpolationMode.Linear,
            KeyFrames =
            {
                new AnimationTrackProperty.KeyFrame(lightStart, 0),
                new AnimationTrackProperty.KeyFrame(lightFinish, animLength),
            }
        });
        anim.AnimationTracks.Add(new AnimationTrackComponentProperty
        {
            ComponentType = typeof(PointLightComponent),
            Property = nameof(PointLightComponent.AnimatedEnable),
            InterpolationMode = AnimationInterpolationMode.Linear,
            KeyFrames =
            {
                new AnimationTrackProperty.KeyFrame(true, 0),
                new AnimationTrackProperty.KeyFrame(!ending, animLength),
            }
        });

        _animPlayer.Play(ent, anim, animationKey);
        _light.SetEnabled(uid, ending);
    }
    #endregion

    #region Siphon Visuals
    [SubscribeNetworkEvent]
    private void OnSiphon(SiphonVisualsEvent args)
    {
        var ent = GetEntity(args.Target);
        var layer = _sprite.AddLayer(ent, new SpriteSpecifier.Rsi(_rsiPath, "vfx"));
        _sprite.LayerMapSet(ent, CultSiphonedVisuals.Key, layer);
        _sprite.LayerSetOffset(ent, layer, new Vector2(0, 0.8f));
        _sprite.LayerSetScale(ent, layer, new Vector2(0.65f, 0.65f));
        if (TryComp<SpriteComponent>(ent, out var sprite))
            sprite.LayerSetShader(layer, "unshaded");

        Timer.Spawn(TimeSpan.FromSeconds(1), () => _sprite.RemoveLayer(ent, CultSiphonedVisuals.Key));
        Audio.PlayLocal(_siphonSfx, ent, ent, AudioParams.Default.WithVariation(0.1f));
    }
    #endregion

    #region Layer Additions
    [SubscribeLocalEvent]
    private void OnCosmicStarMarkAdded(Entity<CosmicStarMarkComponent> uid, ref ComponentStartup args)
    {
        if (_sprite.LayerMapTryGet(uid.Owner, CosmicRevealedKey.Key, out _, false) || !TryComp<SpriteComponent>(uid.Owner, out var sprite))
            return;

        var layer = _sprite.AddLayer(uid.Owner, uid.Comp.Sprite);
        _sprite.LayerMapSet(uid.Owner, CosmicRevealedKey.Key, layer);
        sprite.LayerSetShader(layer, "unshaded");
    }

    [SubscribeLocalEvent]
    private void OnCosmicImpositionAdded(Entity<CosmicImposingComponent> uid, ref ComponentStartup args)
    {
        if (_sprite.LayerMapTryGet(uid.Owner, CosmicImposingKey.Key, out _, false) || !TryComp<SpriteComponent>(uid.Owner, out var sprite))
            return;

        var layer = _sprite.AddLayer(uid.Owner, uid.Comp.Sprite);
        _sprite.LayerMapSet(uid.Owner, CosmicImposingKey.Key, layer);
        sprite.LayerSetShader(layer, "unshaded");
    }
    #endregion

    #region Layer Removals
    [SubscribeLocalEvent]
    private void OnCosmicStarMarkRemoved(Entity<CosmicStarMarkComponent> uid, ref ComponentShutdown args)
    {
        _sprite.RemoveLayer(uid.Owner, CosmicRevealedKey.Key);
    }

    [SubscribeLocalEvent]
    private void OnCosmicImpositionRemoved(Entity<CosmicImposingComponent> uid, ref ComponentShutdown args)
    {
        _sprite.RemoveLayer(uid.Owner, CosmicImposingKey.Key);
    }
    #endregion

    #region Icons
    [SubscribeLocalEvent]
    private void GetCosmicCultIcon(Entity<CosmicCultistComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    [SubscribeLocalEvent]
    private void GetCosmicSSDIcon(Entity<CosmicShuntedOriginComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
    #endregion
}

public enum CultSiphonedVisuals : byte
{
    Key
}
