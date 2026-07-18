using Content.Shared.Inventory;
using Robust.Client.Animus.Conditions;
using Robust.Shared.Prototypes;

namespace Content.Client.Animus.Conditions;

public sealed partial class AnimusConditionHasEquipped : AnimusConditionBase
{
    /// <summary>
    /// Slot to check i.e.: shoes
    /// </summary>
    [DataField]
    public string Slot;

    /// <summary>
    /// Prototype ID of the item required in the specified slot.
    /// </summary>
    [DataField]
    public ProtoId<IPrototype> Prototype;

    private InventorySystem _inventorySystem = null!;
    private IEntityManager _entities = null!;

    public override void Initialize(IEntityManager entityManager)
    {
        base.Initialize(entityManager);
        _entities = entityManager;
        _inventorySystem = entityManager.System<InventorySystem>();
    }

    protected override bool Evaluate(EntityUid entity)
    {
        if (!_inventorySystem.TryGetSlotEntity(entity, Slot, out var slotEntity))
            return false;

        if (!_entities.TryGetComponent<MetaDataComponent>(slotEntity, out var metaData))
            return false;

        if (metaData.EntityPrototype == null)
            return false;

        return metaData.EntityPrototype.ID == Prototype;
    }
}
