// SPDX-FileCopyrightText: 2025 Drywink <43855731+Drywink@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Drywink <hugogrethen@gmail.com>
//
// SPDX-License-Identifier: MIT

using System.Numerics;
using Content.Shared.Interaction.Events;
using Content.Shared.Species.Arachnid;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Containers;
using Robust.Shared.Maths;

namespace Content.Client.Species;

public sealed class CocoonVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private const string CocoonContainerId = "cocoon_victim";

    /// <summary>
    /// Dictionary mapping entity prototype IDs to Vector2 scale values for cocoon sprites.
    /// </summary>
    private static readonly Dictionary<string, Vector2> SpeciesCocoonScales = new()
    {
        { "MobHuman", new Vector2(1.0f, 1.0f) },
        { "MobReptilian", new Vector2(1.0f, 1.0f) },
        { "MobMoth", new Vector2(1.0f, 1.0f) },
        { "MobDwarf", new Vector2(1.0f, 0.8f) },
        { "MobSlime", new Vector2(1.0f, 1.0f) },
        { "MobVox", new Vector2(1.1f, 1.1f) },
        { "MobSkeleton", new Vector2(1.0f, 1.0f) },
        { "MobDiona", new Vector2(1.0f, 1.0f) },
        { "MobFelinid", new Vector2(1.0f, 1.0f) },
        { "MobArachnid", new Vector2(1.1f, 1.0f) },
        { "MobGingerbread", new Vector2(1.0f, 1.0f) },
        { "MobThaven", new Vector2(1.0f, 1.2f) },
    };

    /// <summary>
    /// Default scale to use when species is not found in the dictionary.
    /// </summary>
    private static readonly Vector2 DefaultCocoonScale = new Vector2(1.0f, 1.0f);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CocoonRotationAnimationEvent>(OnCocoonRotationAnimation);
        SubscribeLocalEvent<CocoonedComponent, AttackAttemptEvent>(OnCocoonedAttackAttempt);
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
        if (!Exists(victim) || !TryComp<MetaDataComponent>(victim, out var metaData)
            || metaData.EntityPrototype == null)
            return;

        sprite.Scale = GetScaleForSpecies(metaData.EntityPrototype.ID);
    }

    /// <summary>
    /// Gets the Vector2 scale for a given entity prototype ID.
    /// Returns the default scale if the prototype ID is not found in the dictionary.
    /// </summary>
    private static Vector2 GetScaleForSpecies(string entityProtoId)
    {
        return SpeciesCocoonScales.TryGetValue(entityProtoId, out var scale)
            ? scale
            : DefaultCocoonScale;
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

    private void OnCocoonedAttackAttempt(Entity<CocoonedComponent> ent, ref AttackAttemptEvent args)
    {
        // This prevents the client-side attack animation from playing
        args.Cancel();
    }
}
