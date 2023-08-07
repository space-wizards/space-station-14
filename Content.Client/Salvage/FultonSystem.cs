using System.Numerics;
using Content.Shared.Salvage.Fulton;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Salvage;

public sealed class FultonSystem : SharedFultonSystem
{
    [Dependency] private readonly AnimationPlayerSystem _player = default!;

    private static readonly TimeSpan AnimationDuration = TimeSpan.FromSeconds(0.4);

    private static readonly Animation Animation = new()
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

    [ValidatePrototypeId<EntityPrototype>] private const string EffectProto = "FultonEffect";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FultonedComponent, ComponentStartup>(OnFultonedStartup);
        SubscribeLocalEvent<FultonedComponent, ComponentShutdown>(OnFultonedShutdown);
    }

    private void OnFultonedShutdown(EntityUid uid, FultonedComponent component, ComponentShutdown args)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        QueueDel(component.Effect);
    }

    private void OnFultonedStartup(EntityUid uid, FultonedComponent component, ComponentStartup args)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        component.Effect = Spawn(EffectProto, new EntityCoordinates(uid, Vector2.Zero));
        UpdateAppearance(uid, component);
    }

    protected override void UpdateAppearance(EntityUid uid, FultonedComponent component)
    {
        var startTime = component.NextFulton - FultonDuration;
        var elapsed = Timing.CurTime - startTime;

        if (elapsed >= AnimationDuration)
        {
            return;
        }

        _player.Play(component.Effect, Animation, "fulton");
    }

    [UsedImplicitly]
    public enum FultonVisualLayers : byte
    {
        Base,
    }
}
