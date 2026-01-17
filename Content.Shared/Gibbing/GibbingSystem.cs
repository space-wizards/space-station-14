using Content.Shared.Destructible;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Shared.Gibbing;

public sealed class GibbingSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly SoundSpecifier? GibSound = new SoundCollectionSpecifier("gib", AudioParams.Default.WithVariation(0.025f));

    /// <summary>
    /// Gibs an entity.
    /// </summary>
    /// <param name="ent">The entity to gib.</param>
    /// <param name="dropGiblets">Whether or not to drop giblets.</param>
    /// <param name="user">The user gibbing the entity, if any.</param>
    /// <returns>The set of giblets for this entity, if any.</returns>
    public HashSet<EntityUid> Gib(EntityUid ent, bool dropGiblets = true, EntityUid? user = null)
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
                FlingDroppedEntity(giblet);
            }
        }

        var beforeDeletion = new GibbedBeforeDeletionEvent(gibbed);
        RaiseLocalEvent(ent, ref beforeDeletion);

        return gibbed;
    }

    private const float GibletLaunchImpulse = 8;
    private const float GibletLaunchImpulseVariance = 3;

    private void FlingDroppedEntity(EntityUid target)
    {
        var impulse = GibletLaunchImpulse + _random.NextFloat(GibletLaunchImpulseVariance);
        var scatterVec = _random.NextAngle().ToVec() * impulse;
        _physics.ApplyLinearImpulse(target, scatterVec);
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
