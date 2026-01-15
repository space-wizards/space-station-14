using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Content.Shared.VendingMachines;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
///     Spawns a portion of the total items from one of the canRestock
///     inventory entries on a VendingMachineRestock component.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class DumpRestockInventory : IThresholdBehavior
{
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    ///     The percent of each inventory entry that will be salvaged
    ///     upon destruction of the package.
    /// </summary>
    [DataField(required: true)]
    public float Percent = 0.5f;

    [DataField]
    public float Offset { get; set; } = 0.5f;

    public void Execute(EntityUid owner, SharedDestructibleSystem system, EntityUid? cause = null)
    {
        if (!system.EntityManager.TryGetComponent<VendingMachineRestockComponent>(owner, out var packagecomp) ||
            !system.EntityManager.TryGetComponent<TransformComponent>(owner, out var xform))
            return;

        var randomInventory = _random.Pick(packagecomp.CanRestock);

        if (!_prototypeManager.TryIndex(randomInventory, out VendingMachineInventoryPrototype? packPrototype))
            return;

        foreach (var (entityId, count) in packPrototype.StartingInventory)
        {
            var toSpawn = (int)Math.Round(count * Percent);

            if (toSpawn == 0) continue;

            if (EntityPrototypeHelpers.HasComponent<StackComponent>(entityId, _prototypeManager, system.EntityManager.ComponentFactory))
            {
                var spawned = system.EntityManager.SpawnEntity(entityId, xform.Coordinates.Offset(_random.NextVector2(-Offset, Offset)));
                _stack.SetCount((spawned, null), toSpawn);
                system.EntityManager.GetComponent<TransformComponent>(spawned).LocalRotation = _random.NextAngle();
            }
            else
            {
                for (var i = 0; i < toSpawn; i++)
                {
                    var spawned = system.EntityManager.SpawnEntity(entityId, xform.Coordinates.Offset(_random.NextVector2(-Offset, Offset)));
                    system.EntityManager.GetComponent<TransformComponent>(spawned).LocalRotation = _random.NextAngle();
                }
            }
        }
    }
}
