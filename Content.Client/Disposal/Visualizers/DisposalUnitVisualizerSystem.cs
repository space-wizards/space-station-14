using Robust.Client.Animations;
using Robust.Client.GameObjects;
using static Content.Shared.Disposal.Components.SharedDisposalUnitComponent;

namespace Content.Client.Disposal.Visualizers;

public sealed class DisposalUnitVisualizerSystem : VisualizerSystem<DisposalUnitVisualizerComponent>
{
    #region Dependencies
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    #endregion Dependencies
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DisposalUnitVisualizerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, DisposalUnitVisualizerComponent comp, ComponentInit args)
    {
        comp.FlushAnimation = new Animation {Length = TimeSpan.FromSeconds(comp.FlushTime)};

        var flick = new AnimationTrackSpriteFlick();
        comp.FlushAnimation.AnimationTracks.Add(flick);
        flick.LayerKey = DisposalUnitVisualLayers.Base;
        flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(comp.StateFlush, 0));

        var sound = new AnimationTrackPlaySound();
        comp.FlushAnimation.AnimationTracks.Add(sound);

        sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(comp.FlushSound.GetSound(), 0));
    }

    protected override void OnAppearanceChange(EntityUid uid, DisposalUnitVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;
        if (sprite == null)
            return;
        if (!AppearanceSystem.TryGetData(uid, Visuals.VisualState, out VisualState state, args.Component))
            return;

        switch (state)
        {
            case VisualState.UnAnchored:
                sprite.LayerSetState(DisposalUnitVisualLayers.Base, comp.StateUnAnchored);
                break;
            case VisualState.Anchored:
                sprite.LayerSetState(DisposalUnitVisualLayers.Base, comp.StateAnchored);
                break;
            case VisualState.Charging:
                sprite.LayerSetState(DisposalUnitVisualLayers.Base, comp.StateCharging);
                break;
            case VisualState.Flushing:
                sprite.LayerSetState(DisposalUnitVisualLayers.Base, comp.StateAnchored);

                if (TryComp<AnimationPlayerComponent>(uid, out var animPlayer))
                {
                    if (AnimationSystem.HasRunningAnimation(uid, animPlayer, DisposalUnitVisualizerComponent.AnimationKey))
                        AnimationSystem.Play(uid, animPlayer, comp.FlushAnimation, DisposalUnitVisualizerComponent.AnimationKey);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (!AppearanceSystem.TryGetData(uid, Visuals.Handle, out HandleState handleState, args.Component))
            handleState = HandleState.Normal;

        sprite.LayerSetVisible(DisposalUnitVisualLayers.Handle, handleState != HandleState.Normal);

        switch (handleState)
        {
            case HandleState.Normal:
                break;
            case HandleState.Engaged:
                sprite.LayerSetState(DisposalUnitVisualLayers.Handle, comp.OverlayEngaged);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (!AppearanceSystem.TryGetData(uid, Visuals.Light, out LightState lightState, args.Component))
            lightState = LightState.Off;

        sprite.LayerSetVisible(DisposalUnitVisualLayers.Light, lightState != LightState.Off);

        switch (lightState)
        {
            case LightState.Off:
                break;
            case LightState.Charging:
                sprite.LayerSetState(DisposalUnitVisualLayers.Light, comp.OverlayCharging);
                break;
            case LightState.Full:
                sprite.LayerSetState(DisposalUnitVisualLayers.Light, comp.OverlayFull);
                break;
            case LightState.Ready:
                sprite.LayerSetState(DisposalUnitVisualLayers.Light, comp.OverlayReady);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

public enum DisposalUnitVisualLayers : byte
{
    Base,
    Handle,
    Light
}
