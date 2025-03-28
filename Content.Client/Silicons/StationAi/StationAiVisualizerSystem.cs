using Content.Shared.Silicons.StationAi;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiVisualizerSystem : VisualizerSystem<StationAiCoreComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationAiCoreComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private static readonly Animation CoreUpAnimation = new()
    {
        Length = TimeSpan.FromSeconds(0.8f),
        AnimationTracks =
        {
            new AnimationTrackSpriteFlick
            {
                LayerKey = StationAiVisualLayers.CoreStanding,
                KeyFrames =
                {
                    new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId("up"), 0f)
                }
            },
            new AnimationTrackSpriteFlick
            {
                LayerKey = StationAiVisualLayers.ScreenStanding,
                KeyFrames =
                {
                    new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId("ai_up"), 0f)
                }
            }
        }
    };

    private static readonly Animation CoreDownAnimation = new()
    {
        Length = TimeSpan.FromSeconds(0.8f),
        AnimationTracks =
        {
            new AnimationTrackSpriteFlick
            {
                LayerKey = StationAiVisualLayers.CoreStanding,
                KeyFrames =
                {
                    new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId("down"), 0f)
                }
            },
            new AnimationTrackSpriteFlick
            {
                LayerKey = StationAiVisualLayers.ScreenStanding,
                KeyFrames =
                {
                    new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId("ai_down"), 0f)
                }
            }
        }
    };

    protected override void OnAppearanceChange(EntityUid uid, StationAiCoreComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if (!TryComp<AnimationPlayerComponent>(uid, out var animPlayer))
            return;

        if (!AppearanceSystem.TryGetData<StationAiState>(uid, StationAiVisualState.Key, out var state, args.Component))
            state = StationAiState.Empty;

        UpdateVisuals(uid, state, component, args.Sprite, animPlayer);
    }

    private void OnAnimationCompleted(EntityUid uid, StationAiCoreComponent comp, AnimationCompletedEvent args)
    {
        if (args.Key != StationAiCoreComponent.AnimationKey)
            return;
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;
        if (!TryComp<AnimationPlayerComponent>(uid, out var animPlayer))
            return;

        if (!AppearanceSystem.TryGetData<StationAiState>(uid, StationAiVisualState.Key, out var state))
            state = comp.CurrentState;

        // Convert to finished state
        StationAiState targetState;
        switch (state)
        {
            case StationAiState.Up:
                targetState = StationAiState.Standing;
                break;
            case StationAiState.Down:
                targetState = StationAiState.Occupied;
                break;
            default:
                targetState = state;
                break;
        }

        UpdateVisuals(uid, targetState, comp, sprite, animPlayer);
    }

    private void UpdateVisuals(EntityUid uid, StationAiState state, StationAiCoreComponent comp, SpriteComponent sprite, AnimationPlayerComponent? animPlayer = null)
    {
        if (state == comp.CurrentState)
            return;
        if (!Resolve(uid, ref animPlayer))
            return;
        if (AnimationSystem.HasRunningAnimation(uid, animPlayer, StationAiCoreComponent.AnimationKey))
            return;

        var targetState = state;
        if (comp.CurrentState == StationAiState.Occupied && state == StationAiState.Standing)
            targetState = StationAiState.Up; // play standing up animation first
        else if (comp.CurrentState == StationAiState.Standing && state == StationAiState.Occupied)
            targetState = StationAiState.Down; // play sitting down animation first

        comp.CurrentState = targetState;

        switch (targetState)
        {
            case StationAiState.Empty:
                sprite.LayerSetState(StationAiVisualLayers.Screen, new RSI.StateId("ai_empty"));
                break;
            case StationAiState.Occupied:
                sprite.LayerSetState(StationAiVisualLayers.Screen, new RSI.StateId("ai"));
                break;
            case StationAiState.Dead:
                sprite.LayerSetVisible(StationAiVisualLayers.Core, true);
                sprite.LayerSetVisible(StationAiVisualLayers.Screen, true);
                sprite.LayerSetVisible(StationAiVisualLayers.CoreStanding, false);
                sprite.LayerSetVisible(StationAiVisualLayers.ScreenStanding, false);
                sprite.LayerSetState(StationAiVisualLayers.Screen, new RSI.StateId("ai_dead"));
                break;
            case StationAiState.Up:
                sprite.LayerSetVisible(StationAiVisualLayers.Core, false);
                sprite.LayerSetVisible(StationAiVisualLayers.Screen, false);
                sprite.LayerSetVisible(StationAiVisualLayers.CoreStanding, true);
                sprite.LayerSetVisible(StationAiVisualLayers.ScreenStanding, true);
                AnimationSystem.Play((uid, animPlayer), CoreUpAnimation, StationAiCoreComponent.AnimationKey);
                break;
            case StationAiState.Down:
                sprite.LayerSetVisible(StationAiVisualLayers.Core, false);
                sprite.LayerSetVisible(StationAiVisualLayers.Screen, false);
                sprite.LayerSetVisible(StationAiVisualLayers.CoreStanding, true);
                sprite.LayerSetVisible(StationAiVisualLayers.ScreenStanding, true);
                AnimationSystem.Play((uid, animPlayer), CoreDownAnimation, StationAiCoreComponent.AnimationKey);
                break;
            case StationAiState.Standing:
                sprite.LayerSetState(StationAiVisualLayers.CoreStanding, new RSI.StateId("base_high"));
                sprite.LayerSetState(StationAiVisualLayers.ScreenStanding, new RSI.StateId("ai_high"));
                break;
            default:
                break;
        }
    }
}
