using System.Linq;
using Content.Server.Mech.Systems;
using Content.Shared.Construction;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Vehicle;
using JetBrains.Annotations;
using Robust.Server.Containers;

namespace Content.Server.Construction.Completions;

/// <summary>
/// Drops equipment and modules, removes battery, then changes the entity to its chassis prototype.
/// </summary>
[UsedImplicitly, DataDefinition]
public sealed partial class DisassembleMech : IGraphAction
{
    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent(uid, out MechComponent? mech))
            return;

        // Require cabin open by ensuring no pilot/operator
        var vehicle = entityManager.System<VehicleSystem>();
        if (vehicle.HasOperator(uid))
            return;

        var containerSystem = entityManager.EntitySysManager.GetEntitySystem<ContainerSystem>();

        // Drop equipment and modules
        foreach (var ent in mech.EquipmentContainer.ContainedEntities.ToArray())
            containerSystem.Remove(ent, mech.EquipmentContainer);

        foreach (var ent in mech.ModuleContainer.ContainedEntities.ToArray())
            containerSystem.Remove(ent, mech.ModuleContainer);

        // Change to chassis entity
        if (mech.ChassisPrototype == null)
            return;

        var transform = entityManager.GetComponent<TransformComponent>(uid);
        var chassis = entityManager.SpawnEntity(mech.ChassisPrototype.Value, transform.Coordinates);

        var entChangeEv = new ConstructionChangeEntityEvent(chassis, uid);
        entityManager.EventBus.RaiseLocalEvent(uid, entChangeEv);
        entityManager.EventBus.RaiseLocalEvent(chassis, entChangeEv, broadcast: true);
        entityManager.QueueDeleteEntity(uid);
    }
}


