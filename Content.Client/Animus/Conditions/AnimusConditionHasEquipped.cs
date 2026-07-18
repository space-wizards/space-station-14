using Content.Shared.Inventory;
using Robust.Client.Animus.Conditions;
using Robust.Shared.Prototypes;

namespace Content.Client.Animus.Conditions;

public sealed partial class AnimusConditionHasEquipped : AnimusConditionBase
{
    [DataField]
    public string Slot;

    [DataField]
    public ProtoId<IPrototype> Prototype;

    private InventorySystem _inventorySystem = null!;
    private EntityManager _entities = null!;

    public override void Initialize(EntityManager entityManager)
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
