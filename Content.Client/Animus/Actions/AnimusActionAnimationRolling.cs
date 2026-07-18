using Content.Shared.Movement.Components;
using Robust.Client.Animations;
using Robust.Client.Animus.Actions;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.Animus.Actions;

public sealed partial class AnimusActionAnimationRolling : AnimusActionAnimationBase
{
    ///<summary>
    /// Time for one full rotation. Higher value = slower rotation
    /// </summary>
    [DataField]
    public float RollingPeriod = 0.75f;

    private EntityManager _entities = null!;
    private InputMoverComponent? _inputMoverComponent;

    public override void Initialize(EntityManager entityManager)
    {
        _entities = entityManager;
    }

    protected override Animation? GetNextAnimation(AppearanceSystem appearanceSystem, EntityUid entity, bool restarting)
    {
        if (_inputMoverComponent == null)
        {
            if (!_entities.TryGetComponent<InputMoverComponent>(entity, out var input))
            {
                return null;
            }

            _inputMoverComponent = input;
        }


        var direction = _inputMoverComponent.WishDir.GetDir();
        var directionBool =
            direction is Direction.East or Direction.NorthEast or Direction.SouthEast or Direction.North;
        var anim = new Animation()
        {
            Length = TimeSpan.FromSeconds(RollingPeriod),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(directionBool ? 360 : 0), 0f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(180), RollingPeriod / 2),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(directionBool ? 0 : 360),
                            RollingPeriod / 2),
                    },
                },
            },
        };

        return anim;
    }

    protected override Animation? GetStopAnimation(AppearanceSystem appearanceSystem, EntityUid entity)
    {
        return StopAnimation;
    }
}
