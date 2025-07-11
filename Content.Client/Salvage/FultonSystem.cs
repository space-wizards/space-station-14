using System.Numerics;
using Content.Shared.Salvage.Fulton;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;

namespace Content.Client.Salvage;

public sealed class FultonSystem : SharedFultonSystem
{
    [Dependency] private readonly ISerializationManager _serManager = default!;
    [Dependency] private readonly AnimationPlayerSystem _player = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private static readonly TimeSpan AnimationDuration = TimeSpan.FromSeconds(0.4);

    private static readonly Animation InitialAnimation = new()
    {
        Length = AnimationDuration,
        AnimationTracks =
        {
            new AnimationTrackSpriteFlick
            {
                LayerKey = FultonVisualLayers.Base,
                KeyFrames =
                {
                    new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId("fulton_expand"), 0f),
                    new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId("fulton_balloon"), 0.4f),
                }
            }
        }
    };

    private static readonly Animation FultonAnimation = new()
    {
        Length = TimeSpan.FromSeconds(0.8f),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Offset),
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0f),
                    new AnimationTrackProperty.KeyFrame(new Vector2(0f, -0.3f), 0.3f),
                    new AnimationTrackProperty.KeyFrame(new Vector2(0f, 20f), 0.5f),
                }
            }
        }
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FultonedComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeNetworkEvent<FultonAnimationMessage>(OnFultonMessage);
    }

    private void OnFultonMessage(FultonAnimationMessage ev)
    {
        var entity = GetEntity(ev.Entity);
        var coordinates = GetCoordinates(ev.Coordinates);

        if (Deleted(entity) || !TryComp<SpriteComponent>(entity, out var entSprite))
            return;

        var animationEnt = Spawn(null, coordinates);
        // TODO: Spawn fulton layer
        var sprite = AddComp<SpriteComponent>(animationEnt);
        _serManager.CopyTo(entSprite, ref sprite, notNullableOverride: true);

        if (TryComp<AppearanceComponent>(entity, out var entAppearance))
        {
            var appearance = AddComp<AppearanceComponent>(animationEnt);
            _serManager.CopyTo(entAppearance, ref appearance, notNullableOverride: true);
        }

        sprite.NoRotation = true;
        var effectLayer = _sprite.AddLayer((animationEnt, sprite), new SpriteSpecifier.Rsi(new ResPath("Objects/Tools/fulton_balloon.rsi"), "fulton_balloon"));
        _sprite.LayerSetOffset((animationEnt, sprite), effectLayer, EffectOffset + new Vector2(0f, 0.5f));

        var despawn = AddComp<TimedDespawnComponent>(animationEnt);
        despawn.Lifetime = 1.5f;

        _player.Play(animationEnt, FultonAnimation, "fulton-animation");
    }

    private void OnHandleState(EntityUid uid, FultonedComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateAppearance(uid, component);
    }

    protected override void UpdateAppearance(EntityUid uid, FultonedComponent component)
    {
        if (!component.Effect.IsValid())
            return;

        var startTime = component.NextFulton - component.FultonDuration;
        var elapsed = Timing.CurTime - startTime;

        if (elapsed >= AnimationDuration)
        {
            return;
        }

        _player.Play(component.Effect, InitialAnimation, "fulton");
    }

    [UsedImplicitly]
    public enum FultonVisualLayers : byte
    {
        Base,
    }


}
