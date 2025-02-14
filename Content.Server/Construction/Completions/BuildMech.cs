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
/// Creates the mech entity while transferring all relevant parts inside of it,
/// for right now, the cell that was used in construction.
/// </summary>
[UsedImplicitly, DataDefinition]
public sealed partial class BuildMech : IGraphAction
{
    [DataField("mechPrototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string MechPrototype = string.Empty;

    [DataField("batteryContainer")]
    public string BatteryContainer = "battery-container";
    
    [DataField("gasTankContainer")]
    public string GasTankContainer = "gas-tank-container";

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

        if (!containerSystem.TryGetContainer(uid, BatteryContainer, out var container, containerManager))
        {
            Logger.Warning($"Mech construct entity {uid} did not have the specified '{BatteryContainer}' container! Aborting build mech action.");
            return;
        }
        
        if (!containerSystem.TryGetContainer(uid, GasTankContainer, out var gasTankContainer, containerManager))
        {
            Logger.Warning($"Mech construct entity {uid} did not have the specified '{GasTankContainer}' container! Aborting build mech action.");
            return;
        }

        if (container.ContainedEntities.Count != 1)
        {
            Logger.Warning($"Mech construct entity {uid} did not have exactly one item in the specified '{BatteryContainer}' container! Aborting build mech action.");
        }

        var cell = container.ContainedEntities[0];
        var gasTank = gasTankContainer.ContainedEntities[0];

        if (!entityManager.TryGetComponent<BatteryComponent>(cell, out var batteryComponent))
        {
            Logger.Warning($"Mech construct entity {uid} had an invalid entity in container \"{BatteryContainer}\"! Aborting build mech action.");
            return;
        }

        containerSystem.Remove(cell, container);

        var transform = entityManager.GetComponent<TransformComponent>(uid);
        var mech = entityManager.SpawnEntity(MechPrototype, transform.Coordinates);

        if (entityManager.TryGetComponent<MechComponent>(mech, out var mechComp) && mechComp.BatterySlot.ContainedEntity == null)
        {
            mechSys.InsertBattery(mech, cell, mechComp, batteryComponent);
            containerSystem.Insert(cell, mechComp.BatterySlot);
            if (mechComp.GasTankSlot.ContainedEntity == null && gasTank != null)
                containerSystem.Insert(gasTank, mechComp.GasTankSlot);
        }

        var entChangeEv = new ConstructionChangeEntityEvent(mech, uid);
        entityManager.EventBus.RaiseLocalEvent(uid, entChangeEv);
        entityManager.EventBus.RaiseLocalEvent(mech, entChangeEv, broadcast: true);
        entityManager.QueueDeleteEntity(uid);
    }
}

