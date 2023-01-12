using Content.Shared.Gravity;
using Robust.Client.GameObjects;
using Robust.Client.Animations;
using Robust.Shared.Animations;

namespace Content.Client.Gravity;

public sealed class FloatingVisualizerSystem : VisualizerSystem<FloatingVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GravityChangedEvent>(OnGravityChanged);
        SubscribeLocalEvent<FloatingVisualsComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<FloatingVisualsComponent, EntParentChangedMessage>(OnEntParentChanged);
        SubscribeLocalEvent<FloatingVisualsComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    public void FloatAnimation(EntityUid uid, Vector2 offset, string animationKey, float animationTime)
    {
        var animation = new Animation
        {
            // We multiply by the number of extra keyframes to make time for them
            Length = TimeSpan.FromSeconds(animationTime*2),
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
                        new AnimationTrackProperty.KeyFrame(offset, animationTime),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, animationTime),
                    }
                }
            }
        };

        if (!AnimationSystem.HasRunningAnimation(uid, animationKey))
        {
            AnimationSystem.Play(uid, animation, animationKey);
        }
    }

    private void UpdateAnimation(EntityUid uid, FloatingVisualsComponent component, bool stopAnimation = false, EntityUid? gridUid = null)
    {
        var grid = gridUid ?? Transform(uid).GridUid;
        if (grid == null || !TryComp<GravityComponent>(grid, out var gravity) ||
            !gravity.Enabled)
        {
            FloatAnimation(uid, component.Offset, component.AnimationKey, component.AnimationTime);
            return;
        }

        if (gravity.Enabled && stopAnimation)
        {
            AnimationSystem.Stop(uid, component.AnimationKey);
        }
    }

    private void OnComponentStartup(EntityUid uid, FloatingVisualsComponent component, ComponentStartup args)
    {
        UpdateAnimation(uid, component);
    }

    private void OnGravityChanged(ref GravityChangedEvent args)
    {
        foreach (var component in EntityQuery<FloatingVisualsComponent>())
        {
            var uid = component.Owner;
            UpdateAnimation(uid, component);
        }
    }

    private void OnEntParentChanged(EntityUid uid, FloatingVisualsComponent component, ref EntParentChangedMessage args)
    {
        UpdateAnimation(uid, component, gridUid: args.Transform.GridUid);
    }

    private void OnAnimationCompleted(EntityUid uid, FloatingVisualsComponent component, AnimationCompletedEvent args)
    {
        if (args.Key != component.AnimationKey)
            return;

        UpdateAnimation(uid, component, true);
    }
}
