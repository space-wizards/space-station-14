using Content.Shared.Chemistry.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.Chemistry.Visualizers;

/// <summary>
/// The system responsible for ensuring <see cref="FoamVisualsComponent"/> plays the animation it's meant to when the foam dissolves.
/// </summary>
public sealed class FoamVisualizerSystem : VisualizerSystem<FoamVisualsComponent>
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FoamVisualsComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<FoamVisualsComponent, AnimationCompletedEvent>(OnAnimationComplete);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<FoamVisualsComponent, SmokeComponent>();

        while (query.MoveNext(out var uid, out var comp, out var smoke))
        {
            if (_timing.CurTime < comp.StartTime + TimeSpan.FromSeconds(smoke.Duration) - TimeSpan.FromSeconds(comp.AnimationTime))
                continue;

            // Despawn animation.
            if (TryComp(uid, out AnimationPlayerComponent? animPlayer)
                && !AnimationSystem.HasRunningAnimation(uid, animPlayer, FoamVisualsComponent.AnimationKey))
            {
                AnimationSystem.Play((uid, animPlayer), comp.Animation, FoamVisualsComponent.AnimationKey);
            }
        }
    }

    /// <summary>
    /// Generates the animation used by foam visuals when the foam dissolves.
    /// </summary>
    private void OnComponentInit(EntityUid uid, FoamVisualsComponent comp, ComponentInit args)
    {
        comp.StartTime = _timing.CurTime;
        comp.Animation = new Animation
        {
            Length = TimeSpan.FromSeconds(comp.AnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = FoamVisualLayers.Base,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(comp.AnimationState, 0f)
                    }
                }
            }
        };
    }

    private void OnAnimationComplete(EntityUid uid, FoamVisualsComponent component, AnimationCompletedEvent args)
    {
        if (args.Key != FoamVisualsComponent.AnimationKey)
            return;

        if (TryComp<SpriteComponent>(uid, out var sprite))
            SpriteSystem.SetVisible((uid, sprite), false);
    }
}

public enum FoamVisualLayers : byte
{
    Base
}
