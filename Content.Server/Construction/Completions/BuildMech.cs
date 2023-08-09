using Content.Server.Mech.Components;
using Content.Server.Mech.Systems;
using Content.Server.Power.Components;
using Content.Shared.Construction;
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
public sealed class BuildMech : IGraphAction
{
    [DataField("mechPrototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string MechPrototype = string.Empty;

    [DataField("container")]
    public string Container = "battery-container";

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent(uid, out ContainerManagerComponent? containerManager))
        {
            Logger.Warning($"Mech construct entity {uid} did not have a container manager! Aborting build mech action.");
            return;
        }

        var containerSystem = entityManager.EntitySysManager.GetEntitySystem<ContainerSystem>();
        var mechSys = entityManager.System<MechSystem>();

        if (!containerSystem.TryGetContainer(uid, Container, out var container, containerManager))
        {
            Logger.Warning($"Mech construct entity {uid} did not have the specified '{Container}' container! Aborting build mech action.");
            return;
        }

        if (container.ContainedEntities.Count != 1)
        {
            Logger.Warning($"Mech construct entity {uid} did not have exactly one item in the specified '{Container}' container! Aborting build mech action.");
        }

        var cell = container.ContainedEntities[0];

        if (!entityManager.TryGetComponent<BatteryComponent>(cell, out var batteryComponent))
        {
            Logger.Warning($"Mech construct entity {uid} had an invalid entity in container \"{Container}\"! Aborting build mech action.");
            return;
        }

        container.Remove(cell);

        var transform = entityManager.GetComponent<TransformComponent>(uid);
        var mech = entityManager.SpawnEntity(MechPrototype, transform.Coordinates);

        if (entityManager.TryGetComponent<MechComponent>(mech, out var mechComp) && mechComp.BatterySlot.ContainedEntity == null)
        {
            mechSys.InsertBattery(mech, cell, mechComp, batteryComponent);
            mechComp.BatterySlot.Insert(cell);
        }

        // Delete the original entity.
        entityManager.DeleteEntity(uid);
    }
}

