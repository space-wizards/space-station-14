using Content.Shared.VendingMachines;
using Content.Shared.VendingMachines.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.VendingMachines;

public sealed class VendingMachineSystem : SharedVendingMachineSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VendingMachineVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<VendingMachineVisualsComponent, AnimationCompletedEvent>(OnAnimationCompleted);

        SubscribeLocalEvent<VendingMachineComponent, AfterAutoHandleStateEvent>(OnVendingAfterHandleState);
    }

    private void OnVendingAfterHandleState(Entity<VendingMachineComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        if (UISystem.TryGetOpenUi<VendingMachineBoundUserInterface>(entity.Owner, VendingMachineUiKey.Key, out var bui))
            bui.Update();
    }

    protected override void UpdateUI(Entity<VendingMachineComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        if (UISystem.TryGetOpenUi<VendingMachineBoundUserInterface>(entity.Owner,
                VendingMachineUiKey.Key,
                out var bui))
        {
            bui.UpdateAmounts();
        }
    }

    private void OnAnimationCompleted(Entity<VendingMachineVisualsComponent> ent, ref AnimationCompletedEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!TryComp<AppearanceComponent>(ent, out var appearance) ||
            !_appearanceSystem.TryGetData<VendingMachineVisualState>(ent, VendingMachineVisuals.VisualState, out var visualState, appearance))
        {
            visualState = VendingMachineVisualState.Normal;
        }

        UpdateAppearance(ent, visualState, sprite);
    }

    private void OnAppearanceChange(Entity<VendingMachineVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.AppearanceData.TryGetValue(VendingMachineVisuals.VisualState, out var visualStateObject) ||
            visualStateObject is not VendingMachineVisualState visualState)
        {
            visualState = VendingMachineVisualState.Normal;
        }

        UpdateAppearance(ent, visualState, args.Sprite);
    }

    private void UpdateAppearance(Entity<VendingMachineVisualsComponent> ent, VendingMachineVisualState visualState, SpriteComponent sprite, VendingMachineComponent? vendingMachine = null)
    {
        if (!Resolve(ent, ref vendingMachine))
            return;

        var (uid, component) = ent;

        SetLayerState(VendingMachineVisualLayers.Base, component.OffState, (uid, sprite));

        switch (visualState)
        {
            case VendingMachineVisualState.Normal:
                SetLayerState(VendingMachineVisualLayers.BaseUnshaded, component.NormalState, (uid, sprite));
                SetLayerState(VendingMachineVisualLayers.Screen, component.ScreenState, (uid, sprite));
                break;

            case VendingMachineVisualState.Deny:
                if (component.LoopDenyAnimation)
                    SetLayerState(VendingMachineVisualLayers.BaseUnshaded, component.DenyState, (uid, sprite));
                else
                    PlayAnimation(uid, VendingMachineVisualLayers.BaseUnshaded, component.DenyState, (float)vendingMachine.DenyDelay.TotalSeconds, sprite);

                SetLayerState(VendingMachineVisualLayers.Screen, component.ScreenState, (uid, sprite));
                break;

            case VendingMachineVisualState.Eject:
                PlayAnimation(uid, VendingMachineVisualLayers.BaseUnshaded, component.EjectState, (float)vendingMachine.EjectDelay.TotalSeconds, sprite);
                SetLayerState(VendingMachineVisualLayers.Screen, component.ScreenState, (uid, sprite));
                break;

            case VendingMachineVisualState.Broken:
                HideLayers((uid, sprite));
                SetLayerState(VendingMachineVisualLayers.Base, component.BrokenState, (uid, sprite));
                break;

            case VendingMachineVisualState.Off:
                HideLayers((uid, sprite));
                break;
        }
    }

    private void SetLayerState(VendingMachineVisualLayers layer, string? state, Entity<SpriteComponent> sprite)
    {
        if (string.IsNullOrEmpty(state))
            return;

        _sprite.LayerSetVisible(sprite.AsNullable(), layer, true);
        _sprite.LayerSetAutoAnimated(sprite.AsNullable(), layer, true);
        _sprite.LayerSetRsiState(sprite.AsNullable(), layer, state);
    }

    private void PlayAnimation(EntityUid uid, VendingMachineVisualLayers layer, string? state, float animationTime, SpriteComponent sprite)
    {
        if (string.IsNullOrEmpty(state))
            return;

        if (!_animationPlayer.HasRunningAnimation(uid, state))
        {
            var animation = GetAnimation(layer, state, animationTime);
            _sprite.LayerSetVisible((uid, sprite), layer, true);
            _animationPlayer.Play(uid, animation, state);
        }
    }

    private static Animation GetAnimation(VendingMachineVisualLayers layer, string state, float animationTime)
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

    private void HideLayers(Entity<SpriteComponent> sprite)
    {
        HideLayer(VendingMachineVisualLayers.BaseUnshaded, sprite);
        HideLayer(VendingMachineVisualLayers.Screen, sprite);
    }

    private void HideLayer(VendingMachineVisualLayers layer, Entity<SpriteComponent> sprite)
    {
        if (!_sprite.LayerMapTryGet(sprite.AsNullable(), layer, out var actualLayer, false))
            return;

        _sprite.LayerSetVisible(sprite.AsNullable(), actualLayer, false);
    }
}
