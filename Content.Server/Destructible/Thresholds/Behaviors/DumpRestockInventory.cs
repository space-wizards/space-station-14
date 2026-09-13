using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Stacks;
using Content.Shared.VendingMachines.Components;
using Robust.Shared.Random;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
///     Spawns a portion of the total items from one of the canRestock
///     inventory entries on a VendingMachineRestock component.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class DumpRestockInventory : EntitySystem, IThresholdBehavior
{
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private IRobustRandom _random = default!;

    /// <summary>
    ///     The percent of each inventory entry that will be salvaged
    ///     upon destruction of the package.
    /// </summary>
    [DataField(required: true)]
    public float Percent = 0.5f;

    [DataField]
    public float Offset { get; set; } = 0.5f;

    public void Execute(EntityUid owner, EntityUid? cause = null)
    {
        if (!TryComp<VendingMachineRestockComponent>(owner, out var packagecomp))
            return;

        var randomInventory = _random.Pick(packagecomp.CanRestock);
        if (!ProtoMan.TryIndex(randomInventory, out var packPrototype))
            return;

        foreach (var (entityId, count) in packPrototype.StartingInventory)
        {
            var toSpawn = (int)Math.Round(count * Percent);

            if (toSpawn == 0)
                continue;

            if (ProtoMan.TryIndex(entityId, out var entProto)
                && entProto.HasComp<StackComponent>(EntityManager.ComponentFactory))
            {
                var spawned = SpawnAttachedTo(entityId,
                    Transform(owner).Coordinates.Offset(_random.NextVector2(-Offset, Offset)),
                    rotation: _random.NextAngle());
                _stack.SetCount((spawned, null), toSpawn);
            }
            else
            {
                for (var i = 0; i < toSpawn; i++)
                {
                    SpawnAttachedTo(entityId,
                        Transform(owner).Coordinates.Offset(_random.NextVector2(-Offset, Offset)),
                        rotation: _random.NextAngle());
                }
            }
        }
    }
}
