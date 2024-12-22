using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.Doors;

public sealed class DoorSystem : SharedDoorSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationSystem = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DoorComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    protected override void OnComponentInit(Entity<DoorComponent> door, ref ComponentInit args)
    {
        door.Comp.OpenSpriteStates = new List<(DoorVisualLayers, string)>(2);
        door.Comp.ClosedSpriteStates = new List<(DoorVisualLayers, string)>(2);

        door.Comp.OpenSpriteStates.Add((DoorVisualLayers.Base, door.Comp.OpenSpriteState));
        door.Comp.ClosedSpriteStates.Add((DoorVisualLayers.Base, door.Comp.ClosedSpriteState));

        door.Comp.OpeningAnimation = new Animation()
        {
            Length = TimeSpan.FromSeconds(door.Comp.OpeningAnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = DoorVisualLayers.Base,
                    KeyFrames = {new AnimationTrackSpriteFlick.KeyFrame(door.Comp.OpeningSpriteState, 0f)}
                }
            },
        };

        door.Comp.ClosingAnimation = new Animation()
        {
            Length = TimeSpan.FromSeconds(door.Comp.ClosingAnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = DoorVisualLayers.Base,
                    KeyFrames = {new AnimationTrackSpriteFlick.KeyFrame(door.Comp.ClosingSpriteState, 0f)}
                }
            },
        };

        door.Comp.EmaggingAnimation = new Animation()
        {
            Length = TimeSpan.FromSeconds(door.Comp.EmaggingAnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = DoorVisualLayers.BaseUnlit,
                    KeyFrames = {new AnimationTrackSpriteFlick.KeyFrame(door.Comp.EmaggingSpriteState, 0f)}
                }
            },
        };
    }

    private void OnAppearanceChange(Entity<DoorComponent> door, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<DoorState>(door, DoorVisuals.State, out var state, args.Component))
            state = DoorState.Closed;

        if (AppearanceSystem.TryGetData<string>(door, DoorVisuals.BaseRSI, out var baseRsi, args.Component))
        {
            if (!_resourceCache.TryGetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / baseRsi,
                    out var res))
            {
                Log.Error("Unable to load RSI '{0}'. Trace:\n{1}", baseRsi, Environment.StackTrace);
            }

            foreach (var layer in args.Sprite.AllLayers)
            {
                layer.Rsi = res?.RSI;
            }
        }

        if (!TryComp<AnimationPlayerComponent>(door, out var animPlayer))
        {
            return;
        }

        args.Sprite.DrawDepth = door.Comp.ClosedDrawDepth;

        switch (state)
        {
            case DoorState.Open:
                args.Sprite.DrawDepth = door.Comp.OpenDrawDepth;
                foreach (var (layer, layerState) in door.Comp.OpenSpriteStates)
                {
                    args.Sprite.LayerSetState(layer, layerState);
                }

                return;
            case DoorState.Closed:
                foreach (var (layer, layerState) in door.Comp.ClosedSpriteStates)
                {
                    args.Sprite.LayerSetState(layer, layerState);
                }

                return;
            case DoorState.AttemptingOpenBySelf:
            case DoorState.AttemptingOpenByPrying:
                EndOtherDoorAnimations((door, animPlayer), DoorComponent.AnimationKeyOpen);

                if (door.Comp.OpeningAnimationTime == 0.0 ||
                    _animationSystem.HasRunningAnimation(door, DoorComponent.AnimationKeyOpen))
                    return;

                _animationSystem.Play((door, animPlayer),
                    (Animation)door.Comp.OpeningAnimation,
                    DoorComponent.AnimationKeyOpen);

                return;
            case DoorState.AttemptingCloseBySelf:
            case DoorState.AttemptingCloseByPrying:
                EndOtherDoorAnimations((door, animPlayer), DoorComponent.AnimationKeyClose);

                if (door.Comp.ClosingAnimationTime == 0.0 || door.Comp.CurrentlyCrushing.Count != 0 ||
                    _animationSystem.HasRunningAnimation(door, DoorComponent.AnimationKeyClose))
                    return;

                _animationSystem.Play((door, animPlayer),
                    (Animation)door.Comp.ClosingAnimation,
                    DoorComponent.AnimationKeyClose);

                return;
            case DoorState.Denying:
                EndOtherDoorAnimations((door, animPlayer), DoorComponent.AnimationKeyDeny);

                if (_animationSystem.HasRunningAnimation(door, DoorComponent.AnimationKeyDeny))
                    return;

                _animationSystem.Play((door, animPlayer),
                    (Animation)door.Comp.DenyingAnimation,
                    DoorComponent.AnimationKeyDeny);

                return;
            case DoorState.Emagging:
                EndOtherDoorAnimations((door, animPlayer), DoorComponent.AnimationKeyEmag);

                if (_animationSystem.HasRunningAnimation(door, DoorComponent.AnimationKeyEmag))
                    return;

                _animationSystem.Play((door, animPlayer),
                    (Animation)door.Comp.EmaggingAnimation,
                    DoorComponent.AnimationKeyEmag);

                return;
            case DoorState.ClosingInProgress:
            case DoorState.OpeningInProgress:
            case DoorState.WeldedClosed:
            default:
                return;
        }
    }

    private void EndOtherDoorAnimations(Entity<AnimationPlayerComponent?> door, string key)
    {
        switch (key)
        {
            case DoorComponent.AnimationKeyOpen:
                _animationSystem.Stop(door, DoorComponent.AnimationKeyClose);
                _animationSystem.Stop(door, DoorComponent.AnimationKeyDeny);
                _animationSystem.Stop(door, DoorComponent.AnimationKeyEmag);

                return;
            case DoorComponent.AnimationKeyClose:
                _animationSystem.Stop(door, DoorComponent.AnimationKeyOpen);
                _animationSystem.Stop(door, DoorComponent.AnimationKeyDeny);
                _animationSystem.Stop(door, DoorComponent.AnimationKeyEmag);

                return;
            case DoorComponent.AnimationKeyDeny:
                _animationSystem.Stop(door, DoorComponent.AnimationKeyOpen);
                _animationSystem.Stop(door, DoorComponent.AnimationKeyClose);
                _animationSystem.Stop(door, DoorComponent.AnimationKeyEmag);

                return;
            case DoorComponent.AnimationKeyEmag:
                _animationSystem.Stop(door, DoorComponent.AnimationKeyClose);
                _animationSystem.Stop(door, DoorComponent.AnimationKeyOpen);
                _animationSystem.Stop(door, DoorComponent.AnimationKeyDeny);

                return;
        }
    }
}
