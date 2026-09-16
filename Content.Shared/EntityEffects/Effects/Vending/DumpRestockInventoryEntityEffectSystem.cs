using Content.Shared.Random.Helpers;
using Content.Shared.Stacks;
using Content.Shared.VendingMachines.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.EntityEffects.Effects.Vending;

/// <summary>
/// Spawns a portion of the items from one of the canRestock
/// inventory entries on a <see cref="VendingMachineRestockComponent"/>.
/// Amount is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class DumpRestockInventoryEntityEffectSystem : EntityEffectSystem<TransformComponent, DumpRestockInventory>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedStackSystem _stack = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<DumpRestockInventory> args)
    {
        if (!TryComp<VendingMachineRestockComponent>(entity, out var packagecomp))
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(entity));

        var randomInventory = random.Pick(packagecomp.CanRestock);
        if (!ProtoMan.TryIndex(randomInventory, out var packPrototype))
            return;

        foreach (var (entityId, count) in packPrototype.StartingInventory)
        {
            var toSpawn = (int)MathF.Round(count * args.Effect.Percent * args.Scale);
            if (toSpawn == 0)
                continue;

            var coords = Transform(entity).Coordinates.Offset(random.NextVector2(-args.Effect.Offset, args.Effect.Offset));

            if (ProtoMan.TryIndex(entityId, out var entProto) &&
                entProto.HasComp<StackComponent>(Factory))
            {
                var spawned = PredictedSpawnAttachedTo(entityId, coords, rotation: random.NextAngle());
                _stack.SetCount((spawned, null), toSpawn);
            }
            else
            {
                for (var i = 0; i < toSpawn; i++)
                {
                    PredictedSpawnAttachedTo(entityId, coords, rotation: random.NextAngle());
                }
            }
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class DumpRestockInventory : EntityEffectBase<DumpRestockInventory>
{
    /// <summary>
    /// The percent of each inventory entry that will be salvaged
    /// upon destruction of the package.
    /// </summary>
    [DataField(required: true)]
    public float Percent = 0.5f;

    /// <summary>
    /// Maximum random offset from the package's position at which items are spawned.
    /// </summary>
    [DataField]
    public float Offset { get; set; } = 0.5f;
}
