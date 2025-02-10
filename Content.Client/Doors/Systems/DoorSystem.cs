using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.Doors.Systems;

/// <summary>
/// Controls client-side door behaviour.
/// </summary>
public sealed partial class DoorSystem : SharedDoorSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationSystem = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorComponent, AppearanceChangeEvent>(OnAppearanceChange);

        InitializeClientAirlock();
        InitializeClientDoorAlarm();
    }

    /// <summary>
    /// Initializes door components, setting up door animations.
    /// </summary>
    protected override void OnComponentInit(Entity<DoorComponent> door, ref ComponentInit args)
    {
        base.OnComponentInit(door, ref args);

        door.Comp.OpenSpriteStates = new List<(DoorVisualLayers, string)>(2);
        door.Comp.ClosedSpriteStates = new List<(DoorVisualLayers, string)>(2);

        door.Comp.OpenSpriteStates.Add((DoorVisualLayers.Base, door.Comp.OpenSpriteState));
        door.Comp.ClosedSpriteStates.Add((DoorVisualLayers.Base, door.Comp.ClosedSpriteState));

        door.Comp.OpeningAnimation = new Animation
        {
            Length = TimeSpan.FromSeconds(door.Comp.OpeningAnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = DoorVisualLayers.Base,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(door.Comp.OpeningSpriteState, 0f),
                    },
                },
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
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(door.Comp.ClosingSpriteState, 0f),
                    },
                },
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
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(door.Comp.EmaggingSpriteState, 0f),
                    },
                },
            },
        };
    }

    /// <summary>
    /// Triggers animations and sprite layer states based on door state.
    /// </summary>
    private void OnAppearanceChange(Entity<DoorComponent> door, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<string>(door, DoorVisuals.BaseRSI, out var baseRsi, args.Component))
        {
            UpdateSpriteRSI(args.Sprite, baseRsi);
        }

        if (!TryComp<AnimationPlayerComponent>(door, out var animPlayer))
            return;

        // Sets the draw depth to Closed (above occluding effects like fire) by default.
        args.Sprite.DrawDepth = door.Comp.ClosedDrawDepth;

        switch (door.Comp.State)
        {
            case DoorState.Open:
                // If the door is open, its depth is the open draw depth, which is underneath effects like fire.
                args.Sprite.DrawDepth = door.Comp.OpenDrawDepth;

                // Always stop a closing animation, as Open is directly set if a closing door is blocked from closing.
                // This prevents animation judder.
                _animationSystem.Stop((door, animPlayer), DoorComponent.AnimationKeyClose);

                if (_animationSystem.HasRunningAnimation(door, DoorComponent.AnimationKeyOpen))
                    return;

                foreach (var (layer, layerState) in door.Comp.OpenSpriteStates)
                {
                    args.Sprite.LayerSetState(layer, layerState);
                }

                return;
            case DoorState.Closed:
                if (_animationSystem.HasRunningAnimation(door, DoorComponent.AnimationKeyClose))
                    return;

                foreach (var (layer, layerState) in door.Comp.ClosedSpriteStates)
                {
                    args.Sprite.LayerSetState(layer, layerState);
                }

                return;
            case DoorState.AttemptingOpenBySelf or DoorState.AttemptingOpenByPrying or DoorState.Opening:
                PlayAnimationIfNotPlaying((door, animPlayer),
                    (Animation)door.Comp.OpeningAnimation,
                    DoorComponent.AnimationKeyOpen);

                return;
            case DoorState.AttemptingCloseBySelf or DoorState.AttemptingCloseByPrying or DoorState.Closing:
                PlayAnimationIfNotPlaying((door, animPlayer),
                    (Animation)door.Comp.ClosingAnimation,
                    DoorComponent.AnimationKeyClose);

                return;
            case DoorState.Denying:
                PlayAnimationIfNotPlaying((door, animPlayer),
                    (Animation)door.Comp.DenyingAnimation,
                    DoorComponent.AnimationKeyDeny);

                return;
            case DoorState.Emagging:
                PlayAnimationIfNotPlaying((door, animPlayer),
                    (Animation)door.Comp.EmaggingAnimation,
                    DoorComponent.AnimationKeyEmag);

                return;
        }
    }

    private void UpdateSpriteRSI(SpriteComponent sprite, string baseRsi)
    {
        if (!_resourceCache.TryGetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / baseRsi,
                out var res))
            Log.Error("Unable to load RSI '{0}'. Trace:\n{1}", baseRsi, Environment.StackTrace);

        if (res is null)
            return;

        sprite.BaseRSI = res.RSI;
    }

    private void PlayAnimationIfNotPlaying(Entity<AnimationPlayerComponent> animEntity, Animation animation, string key)
    {
        if (_animationSystem.HasRunningAnimation(animEntity.Owner, key))
            return;

        _animationSystem.Play(animEntity, animation, key);
    }
}
