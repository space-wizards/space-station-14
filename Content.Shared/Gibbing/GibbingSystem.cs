using System.Numerics;
using Content.Shared.Destructible;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Shared.Gibbing;

public sealed partial class GibbingSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDestructibleSystem _destructible = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    private static readonly SoundSpecifier? GibSound = new SoundCollectionSpecifier("gib", AudioParams.Default.WithVariation(0.025f));

    /// <summary>
    /// Gibs an entity.
    /// </summary>
    /// <param name="ent">The entity to gib.</param>
    /// <param name="dropGiblets">Whether or not to drop giblets.</param>
    /// <param name="gibletLaunchImpulse">The force applied to launched giblets.</param>
    /// <param name="gibletLaunchDirection">Direction in which the giblets will launch, in a cone.</param>
    /// <param name="scatterGiblets">Whether to instantly scatter giblets around the entity.</param>
    /// <param name="user">The user gibbing the entity, if any.</param>
    /// <returns>The set of giblets for this entity, if any.</returns>
    public HashSet<EntityUid> Gib(EntityUid ent, bool dropGiblets = true, float gibletLaunchImpulse = 5, Vector2? gibletLaunchDirection = null, Angle throwCone = default, bool scatterGiblets = false, EntityUid? user = null)
    {
        // user is unused because of prediction woes, eventually it'll be used for audio

        // BodySystem handles prediction rather poorly and causes client-sided bugs when we gib on the client
        // This guard can be removed once it is gone and replaced by a prediction-safe system.
        if (!_net.IsServer)
            return new();

        if (!_destructible.DestroyEntity(ent))
            return new();

        _audio.PlayPvs(GibSound, ent);

        var gibbed = new HashSet<EntityUid>();
        var beingGibbed = new BeingGibbedEvent(gibbed);
        RaiseLocalEvent(ent, ref beingGibbed);

        if (dropGiblets)
        {
            foreach (var giblet in gibbed)
            {
                _transform.DropNextTo(giblet, ent);
            }

            _throwing.TryThrowManyRandom(gibbed, gibletLaunchDirection, throwCone, gibletLaunchImpulse, scatterGiblets);
        }

        var beforeDeletion = new GibbedBeforeDeletionEvent(gibbed);
        RaiseLocalEvent(ent, ref beforeDeletion);

        return gibbed;
    }
}

/// <summary>
/// Raised on an entity when it is being gibbed.
/// </summary>
/// <param name="Giblets">If a component wants to provide giblets to scatter, add them to this hashset.</param>
[ByRefEvent]
public readonly record struct BeingGibbedEvent(HashSet<EntityUid> Giblets);

/// <summary>
/// Raised on an entity when it is about to be deleted after being gibbed.
/// </summary>
/// <param name="Giblets">The set of giblets this entity produced.</param>
[ByRefEvent]
public readonly record struct GibbedBeforeDeletionEvent(HashSet<EntityUid> Giblets);
