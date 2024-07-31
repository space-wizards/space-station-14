using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Types;


[Serializable, NetSerializable]
public partial struct ReagentDef
{
    public string Id;

    public bool IsValid => Entity != null && NetEntity != NetEntity.Invalid;

    //This needs to be updated after getting synced from the network (if applicable)
    [NonSerialized]
    public Entity<ReagentDefinitionComponent>? Entity;

    public NetEntity NetEntity;

    public ReagentDef(Entity<ReagentDefinitionComponent> reagentDef, IEntityManager entityManager)
    {
        Id = reagentDef.Comp.Id;
        Entity = reagentDef;
        NetEntity = entityManager.GetNetEntity(reagentDef.Owner);
    }

    public ReagentDef(string id, IEntityManager entityManager, SharedChemistryRegistrySystem? chemRegistry = null)
    {
        chemRegistry ??= entityManager.System<SharedChemistryRegistrySystem>();
        Id = id;
        if (!chemRegistry.TryIndex(id, out var foundReagentDef))
        {
            Entity = null;
            NetEntity = NetEntity.Invalid;
            return;

        }
        Entity = foundReagentDef;
        NetEntity = entityManager.GetNetEntity(foundReagentDef.Value.Owner);
    }

    public void NetSync(IEntityManager entityManager)
    {
        var entId = entityManager.GetEntity(NetEntity);
        if (Entity != null && Entity.Value.Owner == entId)
            return;
        var comp = entityManager.GetComponent<ReagentDefinitionComponent>(entId);
        Entity = (entId, comp);
        Id = comp.Id;
    }

}
