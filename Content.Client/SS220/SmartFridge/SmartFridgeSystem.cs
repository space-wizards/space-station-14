// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.SmartFridge;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.SmartFridge;

public sealed class SmartFridgeSystem : SharedSmartFridgeSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmartFridgeComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<SmartFridgeComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnAnimationCompleted(EntityUid uid, SmartFridgeComponent component, AnimationCompletedEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!TryComp<AppearanceComponent>(uid, out var appearance) ||
            !_appearanceSystem.TryGetData<SmartFridgeVisualState>(uid, SmartFridgeVisuals.VisualState, out var visualState, appearance))
        {
            visualState = SmartFridgeVisualState.Normal;
        }

        UpdateAppearance(uid, visualState, component, sprite);
    }

    private void OnAppearanceChange(EntityUid uid, SmartFridgeComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.AppearanceData.TryGetValue(SmartFridgeVisuals.VisualState, out var visualStateObject) ||
            visualStateObject is not SmartFridgeVisualState visualState)
        {
            visualState = SmartFridgeVisualState.Normal;
        }

        UpdateAppearance(uid, visualState, component, args.Sprite);
    }

    private void UpdateAppearance(EntityUid uid, SmartFridgeVisualState visualState, SmartFridgeComponent component, SpriteComponent sprite)
    {
        SetLayerState(SmartFridgeVisualLayers.Base, component.OffState, sprite);

        switch (visualState)
        {
            case SmartFridgeVisualState.Normal:
                SetLayerState(SmartFridgeVisualLayers.BaseUnshaded, component.NormalState, sprite);
                SetLayerState(SmartFridgeVisualLayers.Screen, component.ScreenState, sprite);
                break;

            case SmartFridgeVisualState.Deny:
                if (component.LoopDenyAnimation)
                    SetLayerState(SmartFridgeVisualLayers.BaseUnshaded, component.DenyState, sprite);
                else
                    PlayAnimation(uid, SmartFridgeVisualLayers.BaseUnshaded, component.DenyState, component.DenyDelay, sprite);

                SetLayerState(SmartFridgeVisualLayers.Screen, component.ScreenState, sprite);
                break;

            case SmartFridgeVisualState.Broken:
                HideLayers(sprite);
                SetLayerState(SmartFridgeVisualLayers.Base, component.BrokenState, sprite);
                break;

            case SmartFridgeVisualState.Off:
                HideLayers(sprite);
                break;
        }
    }

    private static void SetLayerState(SmartFridgeVisualLayers layer, string? state, SpriteComponent sprite)
    {
        if (string.IsNullOrEmpty(state))
            return;

        sprite.LayerSetVisible(layer, true);
        sprite.LayerSetAutoAnimated(layer, true);
        sprite.LayerSetState(layer, state);
    }

    private void PlayAnimation(EntityUid uid, SmartFridgeVisualLayers layer, string? state, float animationTime, SpriteComponent sprite)
    {
        if (string.IsNullOrEmpty(state))
            return;

        if (!_animationPlayer.HasRunningAnimation(uid, state))
        {
            var animation = GetAnimation(layer, state, animationTime);
            sprite.LayerSetVisible(layer, true);
            _animationPlayer.Play(uid, animation, state);
        }
    }

    private static Animation GetAnimation(SmartFridgeVisualLayers layer, string state, float animationTime)
    {
        return new Animation
        {
            Length = TimeSpan.FromSeconds(animationTime),
            AnimationTracks =
                {
                    new AnimationTrackSpriteFlick
                    {
                        LayerKey = layer,
                        KeyFrames =
                        {
                            new AnimationTrackSpriteFlick.KeyFrame(state, 0f)
                        }
                    }
                }
        };
    }

    private static void HideLayers(SpriteComponent sprite)
    {
        HideLayer(SmartFridgeVisualLayers.BaseUnshaded, sprite);
        HideLayer(SmartFridgeVisualLayers.Screen, sprite);
    }

    private static void HideLayer(SmartFridgeVisualLayers layer, SpriteComponent sprite)
    {
        if (!sprite.LayerMapTryGet(layer, out var actualLayer))
            return;

        sprite.LayerSetVisible(actualLayer, false);
    }
}
