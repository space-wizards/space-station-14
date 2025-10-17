using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Doors;

public sealed class DoorSystem : SharedDoorSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationSystem = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DoorComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    protected override void OnComponentInit(Entity<DoorComponent> ent, ref ComponentInit args)
    {
        var comp = ent.Comp;
        comp.OpenSpriteStates = new List<(DoorVisualLayers, string)>(2);
        comp.ClosedSpriteStates = new List<(DoorVisualLayers, string)>(2);

        comp.OpenSpriteStates.Add((DoorVisualLayers.Base, comp.OpenSpriteState));
        comp.ClosedSpriteStates.Add((DoorVisualLayers.Base, comp.ClosedSpriteState));

        comp.OpeningAnimation = new Animation
        {
            Length = comp.OpeningAnimationTime,
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = DoorVisualLayers.Base,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(comp.OpeningSpriteState, 0f),
                    },
                },
            },
        };

        comp.ClosingAnimation = new Animation
        {
            Length = comp.ClosingAnimationTime,
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = DoorVisualLayers.Base,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(comp.ClosingSpriteState, 0f),
                    },
                },
            },
        };

        comp.EmaggingAnimation = new Animation
        {
            Length = comp.EmaggingAnimationTime,
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = DoorVisualLayers.BaseEmagging,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(comp.EmaggingSpriteState, 0f),
                    },
                },
            },
        };
    }

    private void OnAppearanceChange(Entity<DoorComponent> entity, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<DoorState>(entity, DoorVisuals.State, out var state, args.Component))
            state = DoorState.Closed;

        if (AppearanceSystem.TryGetData<string>(entity, PaintableVisuals.Prototype, out var prototype, args.Component))
            UpdateSpriteLayers((entity.Owner, args.Sprite), prototype);

        if (_animationSystem.HasRunningAnimation(entity, DoorComponent.AnimationKey))
            _animationSystem.Stop(entity.Owner, DoorComponent.AnimationKey);

        // We are checking beforehand since some doors may not have an emagging visual layer, and we don't want LayerSetVisible to throw an error.
        if (_sprite.TryGetLayer(entity.Owner, DoorVisualLayers.BaseEmagging, out var _, false))
            _sprite.LayerSetVisible(entity.Owner, DoorVisualLayers.BaseEmagging, state == DoorState.Emagging);

        UpdateAppearanceForDoorState(entity, args.Sprite, state);
    }

    private void UpdateAppearanceForDoorState(Entity<DoorComponent> entity, SpriteComponent sprite, DoorState state)
    {
        _sprite.SetDrawDepth((entity.Owner, sprite), state is DoorState.Open ? entity.Comp.OpenDrawDepth : entity.Comp.ClosedDrawDepth);

        switch (state)
        {
            case DoorState.Open:
                foreach (var (layer, layerState) in entity.Comp.OpenSpriteStates)
                {
                    _sprite.LayerSetRsiState((entity.Owner, sprite), layer, layerState);
                }

                return;
            case DoorState.Closed:
                foreach (var (layer, layerState) in entity.Comp.ClosedSpriteStates)
                {
                    _sprite.LayerSetRsiState((entity.Owner, sprite), layer, layerState);
                }

                return;
            case DoorState.Opening:
                if (entity.Comp.OpeningAnimationTime == TimeSpan.Zero)
                    return;

                _animationSystem.Play(entity, (Animation)entity.Comp.OpeningAnimation, DoorComponent.AnimationKey);

                return;
            case DoorState.Closing:
                if (entity.Comp.ClosingAnimationTime == TimeSpan.Zero || entity.Comp.CurrentlyCrushing.Count != 0)
                    return;

                _animationSystem.Play(entity, (Animation)entity.Comp.ClosingAnimation, DoorComponent.AnimationKey);

                return;
            case DoorState.Denying:
                _animationSystem.Play(entity, (Animation)entity.Comp.DenyingAnimation, DoorComponent.AnimationKey);

                return;
            case DoorState.Emagging:
                // We are checking beforehand since some doors may not have an emagging visual layer.
                if (_sprite.TryGetLayer(entity.Owner, DoorVisualLayers.BaseEmagging, out var _, false))
                    _animationSystem.Play(entity, (Animation)entity.Comp.EmaggingAnimation, DoorComponent.AnimationKey);

                return;
        }
    }

    private void UpdateSpriteLayers(Entity<SpriteComponent> sprite, string targetProto)
    {
        if (!_prototypeManager.Resolve(targetProto, out var target))
            return;

        if (!target.TryGetComponent(out SpriteComponent? targetSprite, _componentFactory))
            return;

        _sprite.SetBaseRsi(sprite.AsNullable(), targetSprite.BaseRSI);
    }
}
