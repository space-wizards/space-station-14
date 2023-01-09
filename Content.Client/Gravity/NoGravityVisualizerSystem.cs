using Content.Shared.Gravity;
using Robust.Client.GameObjects;
using Robust.Client.Animations;
using Robust.Shared.Animations;

namespace Content.Client.Gravity;

public sealed partial class NoGravityVisualizerSystem : VisualizerSystem<NoGravityVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NoGravityVisualsComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<NoGravityVisualsComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnComponentStartup(EntityUid uid, NoGravityVisualsComponent component, ComponentStartup args)
    {
        component.Animation = new Animation
        {
            // We multiply by the number of extra keyframes to make time for them
            Length = TimeSpan.FromSeconds(component.AnimationTime*2),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0f),
                        new AnimationTrackProperty.KeyFrame(component.Offset, component.AnimationTime),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, component.AnimationTime),
                    }
                }
            }
        };

    }

    protected override void OnAppearanceChange(EntityUid uid, NoGravityVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || !AppearanceSystem.TryGetData<bool>(uid, GravityVisuals.Enabled, out var enabled))
            return;

        component.Enabled = enabled;

        if (enabled && !AnimationSystem.HasRunningAnimation(uid, component.AnimationKey))
        {
            AnimationSystem.Play(uid, component.Animation, component.AnimationKey);
        }
    }

    private void OnAnimationCompleted(EntityUid uid, NoGravityVisualsComponent component, AnimationCompletedEvent args)
    {
        if (args.Key != component.AnimationKey)
            return;

        if (component.Enabled && !AnimationSystem.HasRunningAnimation(uid, component.AnimationKey))
        {
            AnimationSystem.Play(uid, component.Animation, component.AnimationKey);
        }

        if (!component.Enabled)
        {
            AnimationSystem.Stop(uid, component.AnimationKey);
        }
    }
}
