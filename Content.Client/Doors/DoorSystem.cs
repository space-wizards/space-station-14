using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Timing;

namespace Content.Client.Doors;

public sealed class DoorSystem : SharedDoorSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IClientResourceCache _resourceCache = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DoorComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    protected override void OnComponentInit(Entity<DoorComponent> ent, ref ComponentInit args)
    {
        var comp = ent.Comp;
        comp.OpenSpriteStates = new(2);
        comp.ClosedSpriteStates = new(2);

        comp.OpenSpriteStates.Add((DoorVisualLayers.Base, comp.OpenSpriteState));
        comp.ClosedSpriteStates.Add((DoorVisualLayers.Base, comp.ClosedSpriteState));

        comp.OpeningAnimation = new Animation()
        {
            Length = TimeSpan.FromSeconds(comp.OpeningAnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = DoorVisualLayers.Base,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(comp.OpeningSpriteState, 0f) }
                }
            },
        };

        comp.ClosingAnimation = new Animation()
        {
            Length = TimeSpan.FromSeconds(comp.ClosingAnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = DoorVisualLayers.Base,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(comp.ClosingSpriteState, 0f) }
                }
            },
        };

        comp.EmaggingAnimation = new Animation ()
        {
            Length = TimeSpan.FromSeconds(comp.EmaggingAnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = DoorVisualLayers.BaseUnlit,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(comp.EmaggingSpriteState, 0f) }
                }
            },
        };
    }

    private void OnAppearanceChange(EntityUid uid, DoorComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || !_gameTiming.IsFirstTimePredicted)
            return;

        if(!AppearanceSystem.TryGetData<DoorState>(uid, DoorVisuals.State, out var state, args.Component))
            state = DoorState.Closed;

        if (AppearanceSystem.TryGetData<string>(uid, DoorVisuals.BaseRSI, out var baseRsi, args.Component))
        {
            if (!_resourceCache.TryGetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / baseRsi, out var res))
            {
                Logger.Error("Unable to load RSI '{0}'. Trace:\n{1}", baseRsi, Environment.StackTrace);
            }
            foreach (ISpriteLayer layer in args.Sprite.AllLayers)
            {
                layer.Rsi = res?.RSI;
            }
        }

        TryComp<AnimationPlayerComponent>(uid, out var animPlayer);
        if (_animationSystem.HasRunningAnimation(uid, animPlayer, DoorComponent.AnimationKey))
            _animationSystem.Stop(uid, animPlayer, DoorComponent.AnimationKey); // Halt all running anomations.

        args.Sprite.DrawDepth = comp.ClosedDrawDepth;
        switch(state)
        {
            case DoorState.Open:
                args.Sprite.DrawDepth = comp.OpenDrawDepth;
                foreach(var (layer, layerState) in comp.OpenSpriteStates)
                {
                    args.Sprite.LayerSetState(layer, layerState);
                }
                break;
            case DoorState.Closed:
                foreach(var (layer, layerState) in comp.ClosedSpriteStates)
                {
                    args.Sprite.LayerSetState(layer, layerState);
                }
                break;
            case DoorState.Opening:
                if (animPlayer != null && comp.OpeningAnimationTime != 0.0)
                    _animationSystem.Play(uid, animPlayer, (Animation)comp.OpeningAnimation, DoorComponent.AnimationKey);
                break;
            case DoorState.Closing:
                if (animPlayer != null && comp.ClosingAnimationTime != 0.0 && comp.CurrentlyCrushing.Count == 0)
                    _animationSystem.Play(uid, animPlayer, (Animation)comp.ClosingAnimation, DoorComponent.AnimationKey);
                break;
            case DoorState.Denying:
                if (animPlayer != null && comp.DenyingAnimation != default)
                    _animationSystem.Play(uid, animPlayer, (Animation)comp.DenyingAnimation, DoorComponent.AnimationKey);
                break;
            case DoorState.Welded:
                break;
            case DoorState.Emagging:
                if (animPlayer != null && comp.EmaggingAnimation != default)
                    _animationSystem.Play(uid, animPlayer, (Animation)comp.EmaggingAnimation, DoorComponent.AnimationKey);
                break;
            default:
                throw new ArgumentOutOfRangeException($"Invalid door visual state {state}");
        }
    }

    // TODO AUDIO PREDICT see comments in server-side PlaySound()
    protected override void PlaySound(EntityUid uid, SoundSpecifier soundSpecifier, AudioParams audioParams, EntityUid? predictingPlayer, bool predicted)
    {
        if (GameTiming.InPrediction && GameTiming.IsFirstTimePredicted)
            Audio.PlayEntity(soundSpecifier, Filter.Local(), uid, false, audioParams);
    }
}
