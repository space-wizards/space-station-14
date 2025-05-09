using Content.Server.Mech.Systems;
using Content.Server.Power.Components;
using Content.Shared.Construction;
using Content.Shared.Mech.Components;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Construction.Completions;

/// <summary>
/// Transforms a mech to a different type, this is used for upgrading mechs.
/// Right now, this is only the Ripley.
/// </summary>
[UsedImplicitly, DataDefinition]
public sealed partial class TransformMech : IGraphAction
{
    [DataField("mechPrototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string MechPrototype = string.Empty;

    [DataField("batteryContainer")]
    public string BatteryContainer = "mech-battery-slot";

    [DataField("gasTankContainer")]
    public string GasTankContainer = "mech-gas-tank-slot";
    [DataField("equipmentContainer")]
    public string EquipmentContainer = "mech-equipment-container";

    // TODO use or generalize ConstructionSystem.ChangeEntity();
    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent(uid, out ContainerManagerComponent? containerManager))
        {
            Logger.Warning($"Mech construct entity {uid} did not have a container manager! Aborting build mech action.");
            return;
        }

        var containerSystem = entityManager.EntitySysManager.GetEntitySystem<ContainerSystem>();
        var mechSys = entityManager.System<MechSystem>();

        if (!containerSystem.TryGetContainer(uid, BatteryContainer, out var batteryContainer, containerManager))
        {
            Logger.Warning($"Mech construct entity {uid} did not have the specified '{BatteryContainer}' container! Aborting build mech action.");
            return;
        }

        if (!containerSystem.TryGetContainer(uid, GasTankContainer, out var gasTankContainer, containerManager))
        {
            Logger.Warning($"Mech construct entity {uid} did not have the specified '{GasTankContainer}' container! Aborting build mech action.");
            return;
        }
        if(!containerSystem.TryGetContainer(uid,EquipmentContainer, out var equipmentContainer, containerManager))
        {
            Logger.Warning($"Mech construct entity {uid} did not have the specified '{EquipmentContainer}' container! Aborting build mech action.");
            return;
        }
        var transform = entityManager.GetComponent<TransformComponent>(uid);
        var mech = entityManager.SpawnEntity(MechPrototype, transform.Coordinates);

        if (entityManager.TryGetComponent<MechComponent>(mech, out var mechComp))
        {
            if (batteryContainer.ContainedEntities.Count == 1)
            {
                var cell = batteryContainer.ContainedEntities[0];
                if (!entityManager.TryGetComponent<BatteryComponent>(cell, out var batteryComponent))
                {
                    Logger.Warning($"Mech construct entity {uid} had an invalid entity in container \"{BatteryContainer}\"! Aborting build mech action.");
                    return;
                }
                ;
                containerSystem.Remove(cell, batteryContainer);
                if (mechComp.BatterySlot.ContainedEntity == null)
                {
                    mechSys.InsertBattery(mech, cell, mechComp, batteryComponent);
                    containerSystem.Insert(cell, mechComp.BatterySlot);
                }
            }
            if (mechComp.GasTankSlot.ContainedEntity == null && gasTankContainer.ContainedEntities.Count > 0)
            {
                var gasTank = gasTankContainer.ContainedEntities[0];
                containerSystem.Insert(gasTank, mechComp.GasTankSlot);
            }
            while (equipmentContainer.ContainedEntities.Count > 0)
            {
                var equipment = equipmentContainer.ContainedEntities[0];
                containerSystem.Remove(equipment, equipmentContainer);
                containerSystem.Insert(equipment, mechComp.EquipmentContainer);
            }
        }
        var entChangeEv = new ConstructionChangeEntityEvent(mech, uid);
        entityManager.EventBus.RaiseLocalEvent(uid, entChangeEv);
        entityManager.EventBus.RaiseLocalEvent(mech, entChangeEv, broadcast: true);
        entityManager.QueueDeleteEntity(uid);

    }
}
