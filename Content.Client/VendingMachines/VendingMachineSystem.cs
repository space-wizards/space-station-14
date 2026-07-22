using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.VendingMachines.Components;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.VendingMachines;
using Content.Shared.VendingMachines.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.VendingMachines;

public sealed partial class VendingMachineSystem : SharedVendingMachineSystem
{
    [Dependency] private AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private SharedPowerReceiverSystem _receiver = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnVendingHandleState(Entity<VendingMachineComponent> entity, ref ComponentHandleState args)
    {
        if (args.Current is not VendingMachineComponentState state)
            return;

        var uid = entity.Owner;
        var component = entity.Comp;

        // If all we did was update amounts, then we can leave BUI buttons in place.
        var fullUiUpdate = !component.Inventory.Keys.SequenceEqual(state.Inventory.Keys) ||
                           !component.EmaggedInventory.Keys.SequenceEqual(state.EmaggedInventory.Keys) ||
                           !component.ContrabandInventory.Keys.SequenceEqual(state.ContrabandInventory.Keys) ||
                           component.Contraband != state.Contraband;

        component.Contraband = state.Contraband;
        var brokenChanged = component.Broken != state.Broken;
        component.Broken = state.Broken;

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

        if (brokenChanged)
            TryUpdateVisualState((uid, component));

        if (!TryGetOpenUi(uid, out var bui)) return;
        if (fullUiUpdate)
        {
            bui.Refresh();
        }
        else
        {
            bui.UpdateAmounts();
        }
    }

    [SubscribeLocalEvent]
    private void OnEjectHandleState(Entity<VendingMachineEjectComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        TryUpdateVisualState(entity.Owner);
    }

    protected override void UpdateUI(Entity<VendingMachineComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        if (TryGetOpenUi(entity.Owner, out var bui))
        {
            bui.UpdateAmounts();
        }
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<VendingMachineComponent> entity, ref PowerChangedEvent args)
    {
        TryUpdateVisualState((entity.Owner, entity.Comp));
    }

    [SubscribeLocalEvent]
    private void OnAnimationCompleted(EntityUid uid, VendingMachineVisualsComponent visuals, AnimationCompletedEvent args)
    {
        if (!TryComp<VendingMachineComponent>(uid, out var vend) ||
            !TryComp<SpriteComponent>(uid, out var sprite))
            return;

        TryComp<VendingMachineEjectComponent>(uid, out var eject);
        var visualState = GetVisualState(uid, vend, eject);
        UpdateAppearance(uid, visualState, visuals, eject, sprite);
    }

    [SubscribeLocalEvent]
    private void OnVisualsStartup(Entity<VendingMachineVisualsComponent> entity, ref ComponentStartup args)
    {
        TryUpdateVisualState(entity.Owner);
    }

    protected override void OnEjectStateChanged(Entity<VendingMachineComponent?> entity, VendingMachineEjectComponent? ejectComponent = null)
    {
        TryUpdateVisualState(entity, ejectComponent);
    }

    private void TryUpdateVisualState(Entity<VendingMachineComponent?> entity, VendingMachineEjectComponent? ejectComponent = null)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        Resolve(entity.Owner, ref ejectComponent, false);

        if (!TryComp<VendingMachineVisualsComponent>(entity.Owner, out var visuals) ||
            !TryComp<SpriteComponent>(entity.Owner, out var sprite))
        {
            return;
        }

        var visualState = GetVisualState(entity.Owner, entity.Comp, ejectComponent);
        UpdatePointLight(entity.Owner, visualState);
        UpdateAppearance(entity.Owner, visualState, visuals, ejectComponent, sprite);
    }

    private VendingMachineVisualState GetVisualState(
        EntityUid uid,
        VendingMachineComponent vend,
        VendingMachineEjectComponent? eject)
    {
        if (vend.Broken)
            return VendingMachineVisualState.Broken;

        if (eject?.Ejecting == true)
            return VendingMachineVisualState.Eject;

        if (eject?.Denying == true)
            return VendingMachineVisualState.Deny;

        if (!_receiver.IsPowered(uid))
            return VendingMachineVisualState.Off;

        return VendingMachineVisualState.Normal;
    }

    private void UpdatePointLight(EntityUid uid, VendingMachineVisualState visualState)
    {
        if (!_light.TryGetLight(uid, out var pointLight))
            return;

        var enabled = visualState != VendingMachineVisualState.Broken && visualState != VendingMachineVisualState.Off;
        _light.SetEnabled(uid, enabled, pointLight);
    }

    private void UpdateAppearance(
        EntityUid uid,
        VendingMachineVisualState visualState,
        VendingMachineVisualsComponent visuals,
        VendingMachineEjectComponent? eject,
        SpriteComponent sprite)
    {
        SetLayerState(VendingMachineVisualLayers.Base, visuals.OffState, (uid, sprite));

        switch (visualState)
        {
            case VendingMachineVisualState.Normal:
                SetLayerState(VendingMachineVisualLayers.BaseUnshaded, visuals.NormalState, (uid, sprite));
                SetLayerState(VendingMachineVisualLayers.Screen, visuals.ScreenState, (uid, sprite));
                break;

            case VendingMachineVisualState.Deny:
                if (visuals.LoopDenyAnimation || eject == null)
                    SetLayerState(VendingMachineVisualLayers.BaseUnshaded, visuals.DenyState, (uid, sprite));
                else
                    PlayAnimation(uid, VendingMachineVisualLayers.BaseUnshaded, visuals.DenyState, (float)eject.DenyDelay.TotalSeconds, sprite);

                SetLayerState(VendingMachineVisualLayers.Screen, visuals.ScreenState, (uid, sprite));
                break;

            case VendingMachineVisualState.Eject:
                if (eject == null)
                    SetLayerState(VendingMachineVisualLayers.BaseUnshaded, visuals.EjectState, (uid, sprite));
                else
                    PlayAnimation(uid, VendingMachineVisualLayers.BaseUnshaded, visuals.EjectState, (float)eject.EjectDelay.TotalSeconds, sprite);

                SetLayerState(VendingMachineVisualLayers.Screen, visuals.ScreenState, (uid, sprite));
                break;

            case VendingMachineVisualState.Broken:
                HideLayers((uid, sprite));
                SetLayerState(VendingMachineVisualLayers.Base, visuals.BrokenState, (uid, sprite));
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

        if (_animationPlayer.HasRunningAnimation(uid, state)) return;
        var animation = GetAnimation(layer, state, animationTime);
        _sprite.LayerSetVisible((uid, sprite), layer, true);
        _animationPlayer.Play(uid, animation, state);
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

    private bool TryGetOpenUi(EntityUid uid, [NotNullWhen(true)] out VendingMachineBoundUserInterface? bui)
    {
        return UISystem.TryGetOpenUi(uid, VendingMachineUiKey.Key, out bui);
    }
}
