using System.Numerics;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Interaction.Events;
using Content.Shared.Species.Arachnid;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Containers;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Species;

public sealed class CocoonVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private const string CocoonContainerId = "cocoon_victim";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CocoonRotationAnimationEvent>(OnCocoonRotationAnimation);
    }

    private void OnCocoonRotationAnimation(CocoonRotationAnimationEvent args)
    {
        var cocoon = GetEntity(args.Cocoon);
        HandleCocoonSpawnAnimation(cocoon, args.VictimWasStanding);
    }

    /// <summary>
    /// Handle the cocoon spawn visual setup (scale adjustment and rotation).
    /// Rotation state is set server-side and replicated to all clients.
    /// If victim was already down, we play instant rotation to prevent smooth animation.
    /// </summary>
    public void HandleCocoonSpawnAnimation(EntityUid uid, bool victimWasStanding)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        // Apply species-based scale to the cocoon sprite
        ApplySpeciesBasedScale(uid, sprite);

        // Rotation state is set server-side via AppearanceComponent and replicated to all clients
        // If victim was already down, play instant rotation to prevent smooth animation
        if (!victimWasStanding)
        {
            PlayInstantRotationAnimation(uid, sprite, Angle.FromDegrees(90));
        }
    }

    /// <summary>
    /// Applies scale to the cocoon sprite based on the victim's species.
    /// </summary>
    private void ApplySpeciesBasedScale(EntityUid cocoonUid, SpriteComponent sprite)
    {
        if (!_container.TryGetContainer(cocoonUid, CocoonContainerId, out var container)
            || container.ContainedEntities.Count == 0)
            return;

        var victim = container.ContainedEntities[0];
        if (!Exists(victim))
            return;

        // Get the species from the victim's HumanoidProfileComponent
        if (!TryComp<HumanoidProfileComponent>(victim, out var humanoid))
            return;

        // Get the species prototype and use its cocoon scale, or default to 1.0, 1.0 if not found
        if (_prototype.TryIndex<SpeciesPrototype>(humanoid.Species, out var speciesPrototype))
        {
            sprite.Scale = speciesPrototype.CocoonScale;
        }
        else
        {
            sprite.Scale = new Vector2(1.0f, 1.0f);
        }
    }

    private void PlayInstantRotationAnimation(EntityUid uid, SpriteComponent spriteComp, Angle rotation)
    {
        if (spriteComp.Rotation.Equals(rotation))
            return;

        var animationComp = EnsureComp<AnimationPlayerComponent>(uid);
        const string animationKey = "cocoon-instant-rotate";

        // Stop any existing animation
        if (_animation.HasRunningAnimation(animationComp, animationKey))
        {
            _animation.Stop((uid, animationComp), animationKey);
        }

        // Create instant animation with single keyframe at 90 degrees
        var animation = new Animation
        {
            Length = TimeSpan.Zero,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(rotation, 0)
                    }
                }
            }
        };

        _animation.Play((uid, animationComp), animation, animationKey);
    }
}
