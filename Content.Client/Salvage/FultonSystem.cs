using Content.Shared.Salvage.Fulton;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

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

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FultonedComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, FultonedComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateAppearance(uid, component);
    }

    protected override void UpdateAppearance(EntityUid uid, FultonedComponent component)
    {
        if (!component.Effect.IsValid())
            return;

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
