using Content.Shared.Inventory;

namespace Content.Shared.EntityEffects.Effects.Transform;

/// <summary>
/// Deletes the entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class DeleteEntityEntityEffectSystem : EntityEffectSystem<TransformComponent, DeleteEntity>
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<DeleteEntity> args)
    {
        if (args.Effect.DropInventory)
        {
            foreach (var item in _inventory.GetHandOrInventoryEntities(entity.Owner))
            {
                _transform.DropNextTo(item, entity.Owner);
            }
        }

        PredictedQueueDel(entity);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class DeleteEntity : EntityEffectBase<DeleteEntity>
{
    /// <summary>
    /// Whether to drop the entity's inventory next to it before deleting it.
    /// </summary>
    [DataField]
    public bool DropInventory = true;
}
