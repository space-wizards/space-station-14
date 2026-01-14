using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Shared.Gibbing;

public sealed class GibbingSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly SoundSpecifier? GibSound = new SoundCollectionSpecifier("gib", AudioParams.Default.WithVariation(0.025f));

    /// <summary>
    /// Gibs an entity.
    /// </summary>
    /// <param name="ent">The entity to gib.</param>
    /// <returns>The set of giblets for this entity, if any.</returns>
    public HashSet<EntityUid> Gib(EntityUid ent)
    {
        _audio.PlayPredicted(GibSound, ent, null);

        var gibbed = new HashSet<EntityUid>();
        var beingGibbed = new BeingGibbedEvent(gibbed);
        RaiseLocalEvent(ent, ref beingGibbed);

        foreach (var giblet in gibbed)
        {
            _transform.DropNextTo(giblet, ent);
            FlingDroppedEntity(giblet);
        }

        var beforeDeletion = new GibbedBeforeDeletionEvent(gibbed);
        RaiseLocalEvent(ent, ref beforeDeletion);

        PredictedQueueDel(ent);

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
