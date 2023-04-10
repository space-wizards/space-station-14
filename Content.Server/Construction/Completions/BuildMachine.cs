using System.Linq;
using Content.Server.Construction.Components;
using Content.Shared.Construction;
using Content.Shared.Construction.Components;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class BuildMachine : IGraphAction
    {
        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (!entityManager.TryGetComponent(uid, out ContainerManagerComponent? containerManager))
            {
                Logger.Warning($"Machine frame entity {uid} did not have a container manager! Aborting build machine action.");
                return;
            }

            if (!entityManager.TryGetComponent(uid, out MachineFrameComponent? machineFrame))
            {
                Logger.Warning($"Machine frame entity {uid} did not have a machine frame component! Aborting build machine action.");
                return;
            }

            if (!entityManager.EntitySysManager.GetEntitySystem<MachineFrameSystem>().IsComplete(machineFrame))
            {
                Logger.Warning($"Machine frame entity {uid} doesn't have all required parts to be built! Aborting build machine action.");
                return;
            }

            if (!containerManager.TryGetContainer(MachineFrameComponent.BoardContainerName, out var entBoardContainer))
            {
                Logger.Warning($"Machine frame entity {uid} did not have the '{MachineFrameComponent.BoardContainerName}' container! Aborting build machine action.");
                return;
            }

            if (!containerManager.TryGetContainer(MachineFrameComponent.PartContainerName, out var entPartContainer))
            {
                Logger.Warning($"Machine frame entity {uid} did not have the '{MachineFrameComponent.PartContainerName}' container! Aborting build machine action.");
                return;
            }

            if (entBoardContainer.ContainedEntities.Count != 1)
            {
                Logger.Warning($"Machine frame entity {uid} did not have exactly one item in the '{MachineFrameComponent.BoardContainerName}' container! Aborting build machine action.");
            }

            var board = entBoardContainer.ContainedEntities[0];

            if (!entityManager.TryGetComponent(board, out MachineBoardComponent? boardComponent))
            {
                Logger.Warning($"Machine frame entity {uid} had an invalid entity in container \"{MachineFrameComponent.BoardContainerName}\"! Aborting build machine action.");
                return;
            }

            entBoardContainer.Remove(board);

            var containerSys = entityManager.EntitySysManager.GetEntitySystem<ContainerSystem>();
            var transform = entityManager.GetComponent<TransformComponent>(uid);
            var machine = entityManager.SpawnEntity(boardComponent.Prototype, transform.Coordinates);
            entityManager.GetComponent<TransformComponent>(machine).LocalRotation = transform.LocalRotation;

            var boardContainer = containerSys.EnsureContainer<Container>(machine, MachineFrameComponent.BoardContainerName, out var existed);

            if (existed)
            {
                // Clean that up...
                containerSys.CleanContainer(boardContainer);
            }

            var partContainer = containerSys.EnsureContainer<Container>(machine, MachineFrameComponent.PartContainerName, out existed);

            if (existed)
            {
                // Clean that up, too...
                containerSys.CleanContainer(partContainer);
            }

            boardContainer.Insert(board);

            // Now we insert all parts.
            foreach (var part in entPartContainer.ContainedEntities.ToArray())
            {
                entPartContainer.ForceRemove(part);
                partContainer.Insert(part);
            }

            var constructionSystem = entityManager.EntitySysManager.GetEntitySystem<ConstructionSystem>();
            if (entityManager.TryGetComponent(machine, out ConstructionComponent? construction))
            {
                // We only add these two container. If some construction needs to take other containers into account, fix this.
                constructionSystem.AddContainer(machine, MachineFrameComponent.BoardContainerName, construction);
                constructionSystem.AddContainer(machine, MachineFrameComponent.PartContainerName, construction);
            }

            if (entityManager.TryGetComponent(machine, out MachineComponent? machineComp))
            {
                constructionSystem.RefreshParts(machineComp);
            }

            entityManager.DeleteEntity(uid);
        }
    }
}
