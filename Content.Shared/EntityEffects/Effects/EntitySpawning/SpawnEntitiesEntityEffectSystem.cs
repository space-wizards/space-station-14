using System.Numerics;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Forensics.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.EntityEffects.Effects.EntitySpawning;

/// <summary>
/// Spawns a random amount of entities from a weighted list of prototypes,
/// each given by a min and a max, at a random offset around this entity.
/// Amount is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class SpawnEntitiesEntityEffectSystem : EntityEffectSystem<TransformComponent, SpawnEntities>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedStackSystem _stack = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpawnEntities> args)
    {
        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(entity));

        var owner = entity.Owner;
        var effect = args.Effect;
        var scale = (int)Math.Floor(args.Scale);

        Vector2 GetRandomVector() => new(random.NextFloat(-effect.Offset, effect.Offset), random.NextFloat(-effect.Offset, effect.Offset));

        // Stacked entities multiply their spawn by the owner's stack count.
        var executions = 1;
        if (TryComp<StackComponent>(owner, out var stack))
            executions = stack.Count;

        foreach (var (entityId, minMax) in effect.Spawn)
        {
            for (var execution = 0; execution < executions; execution++)
            {
                var count = minMax.Min >= minMax.Max
                    ? (int)minMax.Min
                    : (int)random.NextFloat(minMax.Min, minMax.Max + 1);
                count *= scale;

                if (count == 0)
                    continue;

                if (ProtoMan.TryIndex(entityId, out var proto) && proto.HasComp<StackComponent>(Factory))
                {
                    var spawned = effect.SpawnInContainer
                        ? PredictedSpawnNextToOrDrop(entityId, owner)
                        : PredictedSpawnAttachedTo(entityId, Transform(owner).Coordinates.Offset(GetRandomVector()));
                    _stack.SetCount((spawned, null), count);

                    TransferForensics(spawned, owner, effect, random);
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        var spawned = effect.SpawnInContainer
                            ? PredictedSpawnNextToOrDrop(entityId, owner)
                            : PredictedSpawnAttachedTo(entityId, Transform(owner).Coordinates.Offset(GetRandomVector()));

                        TransferForensics(spawned, owner, effect, random);
                    }
                }
            }
        }
    }

    private void TransferForensics(EntityUid spawned, EntityUid owner, SpawnEntities effect, IRobustRandom random)
    {
        if (!effect.DoTransferForensics ||
            !TryComp<ForensicsComponent>(owner, out var forensicsComponent))
            return;

        var comp = EnsureComp<ForensicsComponent>(spawned);
        comp.DNAs = forensicsComponent.DNAs;

        if (!random.Prob(0.4f))
            return;

        comp.Fingerprints = forensicsComponent.Fingerprints;
        comp.Fibers = forensicsComponent.Fibers;
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class SpawnEntities : EntityEffectBase<SpawnEntities>
{
    /// <summary>
    /// Entities spawned on applying this effect, from a min to a max.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, MinMax> Spawn = new();

    /// <summary>
    /// Maximum random offset from the owner's position at which entities are spawned.
    /// </summary>
    [DataField]
    public float Offset { get; set; } = 0.5f;

    /// <summary>
    /// Whether to transfer forensics from the owner to the spawned entities.
    /// </summary>
    [DataField("transferForensics")]
    public bool DoTransferForensics;

    /// <summary>
    /// Whether to spawn the entities inside the owner's container,
    /// dropping them next to it if that fails.
    /// </summary>
    [DataField]
    public bool SpawnInContainer;
}
