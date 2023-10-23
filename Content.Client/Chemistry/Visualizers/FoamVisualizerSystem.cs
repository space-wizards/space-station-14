using Content.Shared.Smoking;
using Robust.Shared.Spawners;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Network;
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
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<FoamVisualsComponent, TimedDespawnComponent>();

        while (query.MoveNext(out var uid, out var comp, out var despawn))
        {
            if (despawn.Lifetime > 1f)
                continue;

            // Despawn animation.
            if (TryComp(uid, out AnimationPlayerComponent? animPlayer)
                && !AnimationSystem.HasRunningAnimation(uid, animPlayer, FoamVisualsComponent.AnimationKey))
            {
                AnimationSystem.Play(uid, animPlayer, comp.Animation, FoamVisualsComponent.AnimationKey);
            }
        }
    }

    /// <summary>
    /// Generates the animation used by foam visuals when the foam dissolves.
    /// </summary>
    private void OnComponentInit(EntityUid uid, FoamVisualsComponent comp, ComponentInit args)
    {
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
                        new AnimationTrackSpriteFlick.KeyFrame(comp.State, 0f)
                    }
                }
            }
        };
    }
}

public enum FoamVisualLayers : byte
{
    Base
}
