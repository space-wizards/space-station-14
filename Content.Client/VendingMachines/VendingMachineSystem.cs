using System.Linq;
using Content.Shared.VendingMachines;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.VendingMachines;

public sealed class VendingMachineSystem : SharedVendingMachineSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VendingMachineComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<VendingMachineComponent, AnimationCompletedEvent>(OnAnimationCompleted);
        SubscribeLocalEvent<VendingMachineComponent, ComponentHandleState>(OnVendingHandleState);
    }

    private void OnVendingHandleState(Entity<VendingMachineComponent> entity, ref ComponentHandleState args)
    {
        if (args.Current is not VendingMachineComponentState state)
            return;

        var uid = entity.Owner;
        var component = entity.Comp;

        component.Contraband = state.Contraband;
        component.EjectEnd = state.EjectEnd;
        component.DenyEnd = state.DenyEnd;
        component.DispenseOnHitEnd = state.DispenseOnHitEnd;

        // If all we did was update amounts then we can leave BUI buttons in place.
        var fullUiUpdate = !component.Inventory.Keys.SequenceEqual(state.Inventory.Keys) ||
                           !component.EmaggedInventory.Keys.SequenceEqual(state.EmaggedInventory.Keys) ||
                           !component.ContrabandInventory.Keys.SequenceEqual(state.ContrabandInventory.Keys);

        component.Inventory.Clear();
        component.EmaggedInventory.Clear();
        component.ContrabandInventory.Clear();

        foreach (var entry in state.Inventory)
        {
            component.Inventory.Add(entry.Key, new(entry.Value));
        }

        foreach (var entry in state.EmaggedInventory)
        {
            component.EmaggedInventory.Add(entry.Key, new(entry.Value));
        }

        foreach (var entry in state.ContrabandInventory)
        {
            component.ContrabandInventory.Add(entry.Key, new(entry.Value));
        }

        if (UISystem.TryGetOpenUi<VendingMachineBoundUserInterface>(uid, VendingMachineUiKey.Key, out var bui))
        {
            if (fullUiUpdate)
            {
                bui.Refresh();
            }
            else
            {
                bui.UpdateAmounts();
            }
        }
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

    private void OnAnimationCompleted(EntityUid uid, VendingMachineComponent component, AnimationCompletedEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!TryComp<AppearanceComponent>(uid, out var appearance) ||
            !_appearanceSystem.TryGetData<VendingMachineVisualState>(uid, VendingMachineVisuals.VisualState, out var visualState, appearance))
        {
            visualState = VendingMachineVisualState.Normal;
        }

        UpdateAppearance(uid, visualState, component, sprite);
    }

    private void OnAppearanceChange(EntityUid uid, VendingMachineComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.AppearanceData.TryGetValue(VendingMachineVisuals.VisualState, out var visualStateObject) ||
            visualStateObject is not VendingMachineVisualState visualState)
        {
            visualState = VendingMachineVisualState.Normal;
        }

        UpdateAppearance(uid, visualState, component, args.Sprite);
    }

    private void UpdateAppearance(EntityUid uid, VendingMachineVisualState visualState, VendingMachineComponent component, SpriteComponent sprite)
    {
        SetLayerState(VendingMachineVisualLayers.Base, component.OffState, sprite);

        switch (visualState)
        {
            case VendingMachineVisualState.Normal:
                SetLayerState(VendingMachineVisualLayers.BaseUnshaded, component.NormalState, sprite);
                SetLayerState(VendingMachineVisualLayers.Screen, component.ScreenState, sprite);
                break;

            case VendingMachineVisualState.Deny:
                if (component.LoopDenyAnimation)
                    SetLayerState(VendingMachineVisualLayers.BaseUnshaded, component.DenyState, sprite);
                else
                    PlayAnimation(uid, VendingMachineVisualLayers.BaseUnshaded, component.DenyState, (float)component.DenyDelay.TotalSeconds, sprite);

                SetLayerState(VendingMachineVisualLayers.Screen, component.ScreenState, sprite);
                break;

            case VendingMachineVisualState.Eject:
                PlayAnimation(uid, VendingMachineVisualLayers.BaseUnshaded, component.EjectState, (float)component.EjectDelay.TotalSeconds, sprite);
                SetLayerState(VendingMachineVisualLayers.Screen, component.ScreenState, sprite);
                break;

            case VendingMachineVisualState.Broken:
                HideLayers(sprite);
                SetLayerState(VendingMachineVisualLayers.Base, component.BrokenState, sprite);
                break;

            case VendingMachineVisualState.Off:
                HideLayers(sprite);
                break;
        }
    }

    private static void SetLayerState(VendingMachineVisualLayers layer, string? state, SpriteComponent sprite)
    {
        if (string.IsNullOrEmpty(state))
            return;

        sprite.LayerSetVisible(layer, true);
        sprite.LayerSetAutoAnimated(layer, true);
        sprite.LayerSetState(layer, state);
    }

    private void PlayAnimation(EntityUid uid, VendingMachineVisualLayers layer, string? state, float animationTime, SpriteComponent sprite)
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

    private static void HideLayers(SpriteComponent sprite)
    {
        HideLayer(VendingMachineVisualLayers.BaseUnshaded, sprite);
        HideLayer(VendingMachineVisualLayers.Screen, sprite);
    }

    private static void HideLayer(VendingMachineVisualLayers layer, SpriteComponent sprite)
    {
        if (!sprite.LayerMapTryGet(layer, out var actualLayer))
            return;

        sprite.LayerSetVisible(actualLayer, false);
    }
}
