using System.Linq;
using Content.Server.Atmos.Components;
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
        //starlight, REFACTORED pretty much all of this code. if any of it changes, it will need to be tested again.
        var transform = entityManager.GetComponent<TransformComponent>(uid);
        var newMech = entityManager.SpawnEntity(MechPrototype, transform.Coordinates);
        if (!entityManager.TryGetComponent<MechComponent>(newMech, out var mechComp))
        {
            Logger.Warning($"Mech construct entity {uid} did not have a mech component! Aborting build mech action.");
            return;
        }

        //dear god help me why are the slot IDs different
        TryTransferContainerContents(uid, entityManager, BatteryContainer, mechComp.BatterySlot);
        TryTransferContainerContents(uid, entityManager, GasTankContainer, mechComp.GasTankSlot);

        var entChangeEv = new ConstructionChangeEntityEvent(newMech, uid);
        entityManager.EventBus.RaiseLocalEvent(uid, entChangeEv);
        entityManager.EventBus.RaiseLocalEvent(newMech, entChangeEv, broadcast: true);
        entityManager.QueueDeleteEntity(uid);
    }

    private void TryTransferContainerContents(EntityUid uid, IEntityManager entityManager, string sourceContainerID, ContainerSlot targetSlot)
    {
        if (!entityManager.TryGetComponent(uid, out ContainerManagerComponent? containerManager))
        {
            Logger.Warning($"Mech construct entity {uid} did not have a container manager! Aborting build mech action.");
            return;
        }

        var containerSystem = entityManager.EntitySysManager.GetEntitySystem<ContainerSystem>();

        if (!containerSystem.TryGetContainer(uid, sourceContainerID, out var originalContainer, containerManager))
        {
            return;
        }

        List<EntityUid> EntitiesToTransfer = originalContainer.ContainedEntities.ToList(); //we need to copy the list, as we are modifying the original container.

        foreach (var entity in EntitiesToTransfer)
        {
            if (containerSystem.TryRemoveFromContainer(entity, true, out bool wasInContainer))
            {
                //all other items except the last that we process will just end up on the ground
                containerSystem.Insert(entity, targetSlot);
            }
        }
    }
}
