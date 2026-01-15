using System.Numerics;
using Content.Server.Forensics;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[Serializable]
[DataDefinition]
public sealed partial class SpawnEntitiesBehavior : EntitySystem, IThresholdBehavior
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    /// <summary>
    ///     Entities spawned on reaching this threshold, from a min to a max.
    /// </summary>
    [DataField]
    public new Dictionary<EntProtoId, MinMax> Spawn = new();

    [DataField]
    public float Offset { get; set; } = 0.5f;

    [DataField("transferForensics")]
    public bool DoTransferForensics;

    [DataField]
    public bool SpawnInContainer;

    public void Execute(EntityUid owner, EntityUid? cause = null)
    {
        var position = _transform.GetMapCoordinates(owner);
        var getRandomVector = () => new Vector2(_random.NextFloat(-Offset, Offset), _random.NextFloat(-Offset, Offset));

        var executions = 1;
        if (TryComp<StackComponent>(owner, out var stack))
        {
            executions = stack.Count;
        }

        foreach (var (entityId, minMax) in Spawn)
        {
            for (var execution = 0; execution < executions; execution++)
            {
                var count = minMax.Min >= minMax.Max
                    ? minMax.Min
                    : _random.Next(minMax.Min, minMax.Max + 1);

                if (count == 0)
                    continue;

                if (EntityPrototypeHelpers.HasComponent<StackComponent>(entityId, _prototypeManager, EntityManager.ComponentFactory))
                {
                    var spawned = SpawnInContainer
                        ? SpawnNextToOrDrop(entityId, owner)
                        : Spawn(entityId, position.Offset(getRandomVector()));
                    _stack.SetCount((spawned, null), count);

                    TransferForensics(spawned, owner);
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        var spawned = SpawnInContainer
                            ? SpawnNextToOrDrop(entityId, owner)
                            : Spawn(entityId, position.Offset(getRandomVector()));

                        TransferForensics(spawned, owner);
                    }
                }
            }
        }
    }

    public void TransferForensics(EntityUid spawned, EntityUid owner)
    {
        if (!DoTransferForensics ||
            !TryComp<ForensicsComponent>(owner, out var forensicsComponent))
            return;

        var comp = EnsureComp<ForensicsComponent>(spawned);
        comp.DNAs = forensicsComponent.DNAs;

        if (!_random.Prob(0.4f))
            return;

        comp.Fingerprints = forensicsComponent.Fingerprints;
        comp.Fibers = forensicsComponent.Fibers;
    }
}
