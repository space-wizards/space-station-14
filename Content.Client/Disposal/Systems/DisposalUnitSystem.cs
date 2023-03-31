using Content.Client.Disposal.Components;
using Content.Client.Disposal.UI;
using Content.Shared.Disposal;
using Robust.Client.GameObjects;
using Robust.Client.Animations;
using static Content.Shared.Disposal.Components.SharedDisposalUnitComponent;

namespace Content.Client.Disposal.Systems;

public sealed class DisposalUnitSystem : SharedDisposalUnitSystem
{
    [Dependency] private readonly AppearanceSystem AppearanceSystem = default!;
    [Dependency] private readonly AnimationPlayerSystem AnimationSystem = default!;
    [Dependency] private readonly SharedAudioSystem SoundSystem = default!;
    private const string AnimationKey = "disposal_unit_animation";

    private List<EntityUid> PressuringDisposals = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalUnitComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DisposalUnitComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    public void UpdateActive(EntityUid disposalEntity, bool active)
    {
        if (active)
        {
            if (!PressuringDisposals.Contains(disposalEntity))
                PressuringDisposals.Add(disposalEntity);
        }
        else
        {
            PressuringDisposals.Remove(disposalEntity);
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        for (var i = PressuringDisposals.Count - 1; i >= 0; i--)
        {
            var disposal = PressuringDisposals[i];
            if (!UpdateInterface(disposal))
                continue;

            PressuringDisposals.RemoveAt(i);
        }
    }

    private bool UpdateInterface(EntityUid disposalUnit)
    {
        if (!TryComp(disposalUnit, out DisposalUnitComponent? component) || component.Deleted)
            return true;
        if (component.Deleted)
            return true;
        if (!TryComp(disposalUnit, out ClientUserInterfaceComponent? userInterface))
            return true;

        var state = component.UiState;
        if (state == null)
            return true;

        foreach (var inter in userInterface.Interfaces)
        {
            if (inter is DisposalUnitBoundUserInterface boundInterface)
            {
                return boundInterface.UpdateWindowState(state) != false;
            }
        }

        return true;
    }

    private void OnComponentInit(EntityUid uid, DisposalUnitComponent disposalUnit, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if(!sprite.LayerMapTryGet(DisposalUnitVisualLayers.Base, out var baseLayerIdx))
            return; // Couldn't find the "normal" layer to return to after flush animation

        if(!sprite.LayerMapTryGet(DisposalUnitVisualLayers.BaseFlush, out var flushLayerIdx))
            return; // Couldn't find the flush animation layer

        var originalBaseState = sprite.LayerGetState(baseLayerIdx);
        var flushState = sprite.LayerGetState(flushLayerIdx);

        // Setup the flush animation to play
        disposalUnit.FlushAnimation = new Animation {
            Length = TimeSpan.FromSeconds(disposalUnit.FlushTime),
            AnimationTracks = {
                new AnimationTrackSpriteFlick {
                    LayerKey = DisposalUnitVisualLayers.BaseFlush,
                    KeyFrames = {
                        // Play the flush animation
                        new AnimationTrackSpriteFlick.KeyFrame(flushState, 0),
                        // Return to base state (though, depending on how the unit is
                        // configured we might get an appearence change event telling
                        // us to go to charging state)
                        new AnimationTrackSpriteFlick.KeyFrame(originalBaseState, disposalUnit.FlushTime)
                    }
                },
            }
        };

        if (disposalUnit.FlushSound != null)
        {
            disposalUnit.FlushAnimation.AnimationTracks.Add(
                new AnimationTrackPlaySound {
                    KeyFrames = {
                        new AnimationTrackPlaySound.KeyFrame(SoundSystem.GetSound(disposalUnit.FlushSound), 0)
                    }
                });
        }

        EnsureComp<AnimationPlayerComponent>(uid);

        UpdateState(uid, disposalUnit, sprite);
    }

    private void OnAppearanceChange(EntityUid uid, DisposalUnitComponent unit, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
        {
            return;
        }

        UpdateState(uid, unit, args.Sprite);
    }

    // Update visuals and tick animation
    private void UpdateState(EntityUid uid, DisposalUnitComponent unit, SpriteComponent sprite)
    {
        if (!AppearanceSystem.TryGetData<VisualState>(uid, Visuals.VisualState, out var state))
        {
            return;
        }

        sprite.LayerSetVisible(DisposalUnitVisualLayers.Unanchored, state == VisualState.UnAnchored);
        sprite.LayerSetVisible(DisposalUnitVisualLayers.Base, state == VisualState.Anchored);
        sprite.LayerSetVisible(DisposalUnitVisualLayers.BaseCharging, state == VisualState.Charging);
        sprite.LayerSetVisible(DisposalUnitVisualLayers.BaseFlush, state == VisualState.Flushing);

        if (state == VisualState.Flushing)
        {
            if (!AnimationSystem.HasRunningAnimation(uid, AnimationKey))
            {
                AnimationSystem.Play(uid, unit.FlushAnimation, AnimationKey);
            }
        }

        if (!AppearanceSystem.TryGetData<HandleState>(uid, Visuals.Handle, out var handleState))
        {
            handleState = HandleState.Normal;
        }

        sprite.LayerSetVisible(DisposalUnitVisualLayers.OverlayEngaged, handleState != HandleState.Normal);

        if (!AppearanceSystem.TryGetData<LightStates>(uid, Visuals.Light, out var lightState))
        {
            lightState = LightStates.Off;
        }

        sprite.LayerSetVisible(DisposalUnitVisualLayers.OverlayCharging,
                (lightState & LightStates.Charging) != 0);
        sprite.LayerSetVisible(DisposalUnitVisualLayers.OverlayReady,
                (lightState & LightStates.Ready) != 0);
        sprite.LayerSetVisible(DisposalUnitVisualLayers.OverlayFull,
                (lightState & LightStates.Full) != 0);
    }
}

public enum DisposalUnitVisualLayers : byte
{
    Unanchored,
    Base,
    BaseCharging,
    BaseFlush,
    OverlayCharging,
    OverlayReady,
    OverlayFull,
    OverlayEngaged
}
