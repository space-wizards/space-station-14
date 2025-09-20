using System.Linq;
using Content.Server.Mech.Systems;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Shared.Construction;
using Content.Shared.Construction.Components;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Vehicle;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction.Completions;

/// <summary>
/// Returns a mech to its assembly stage: drops gear and switches to the assembly graph at "start".
/// </summary>
[UsedImplicitly, DataDefinition]
public sealed partial class DisassembleMech : IGraphAction
{

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent(uid, out MechComponent? mech))
            return;

        // Require open cabin (no operator)
        var vehicle = entityManager.System<VehicleSystem>();
        if (vehicle.HasOperator(uid))
            return;

        // Switch to assembly host prototype derived from graph id and prefill parts
        if (string.IsNullOrEmpty(mech.AssemblyGraphId))
            return;

        var hostProto = mech.AssemblyGraphId;

        var xform = entityManager.GetComponent<TransformComponent>(uid);
        var host = entityManager.SpawnEntity(hostProto, xform.Coordinates);

        // Prefill PartAssembly parts using system path so assembly state is consistent with construction.
        if (entityManager.TryGetComponent(host, out PartAssemblyComponent? partAssembly))
        {
            var tags = partAssembly.Parts.Values.FirstOrDefault();
            if (tags != null)
            {
                var protoMan = IoCManager.Resolve<IPrototypeManager>();
                var partAssemblySys = entityManager.System<PartAssemblySystem>();
                foreach (var tagId in tags)
                {
                    if (!protoMan.TryIndex<EntityPrototype>(tagId, out _))
                        continue;

                    var part = entityManager.SpawnEntity(tagId, xform.Coordinates);
                    partAssemblySys.TryInsertPart(part, host, partAssembly);
                }
            }
        }

        var entChangeEv = new ConstructionChangeEntityEvent(host, uid);
        entityManager.EventBus.RaiseLocalEvent(uid, entChangeEv);
        entityManager.EventBus.RaiseLocalEvent(host, entChangeEv, broadcast: true);
        entityManager.QueueDeleteEntity(uid);
    }
}


