#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Construction;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    public class BuildMachine : IGraphAction
    {
        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (!entity.TryGetComponent(out ContainerManagerComponent? containerManager))
            {
                Logger.Warning($"Machine frame entity {entity} did not have a container manager! Aborting build machine action.");
                return;
            }

            if (!entity.TryGetComponent(out MachineFrameComponent? machineFrame))
            {
                Logger.Warning($"Machine frame entity {entity} did not have a machine frame component! Aborting build machine action.");
                return;
            }

            if (!machineFrame.IsComplete)
            {
                Logger.Warning($"Machine frame entity {entity} doesn't have all required parts to be built! Aborting build machine action.");
                return;
            }

            if (!containerManager.TryGetContainer(MachineFrameComponent.BoardContainer, out var entBoardContainer))
            {
                Logger.Warning($"Machine frame entity {entity} did not have the '{MachineFrameComponent.BoardContainer}' container! Aborting build machine action.");
                return;
            }

            if (!containerManager.TryGetContainer(MachineFrameComponent.PartContainer, out var entPartContainer))
            {
                Logger.Warning($"Machine frame entity {entity} did not have the '{MachineFrameComponent.PartContainer}' container! Aborting build machine action.");
                return;
            }

            if (entBoardContainer.ContainedEntities.Count != 1)
            {
                Logger.Warning($"Machine frame entity {entity} did not have exactly one item in the '{MachineFrameComponent.BoardContainer}' container! Aborting build machine action.");
            }

            var board = entBoardContainer.ContainedEntities[0];

            if (!board.TryGetComponent(out MachineBoardComponent? boardComponent))
            {
                Logger.Warning($"Machine frame entity {entity} had an invalid entity in container \"{MachineFrameComponent.BoardContainer}\"! Aborting build machine action.");
                return;
            }

            var entityManager = entity.EntityManager;
            entBoardContainer.Remove(board);

            var machine = entityManager.SpawnEntity(boardComponent.Prototype, entity.Transform.Coordinates);
            machine.Transform.LocalRotation = entity.Transform.LocalRotation;

            var boardContainer = ContainerManagerComponent.Ensure<Container>(MachineFrameComponent.BoardContainer, machine, out var existed);

            if (existed)
            {
                // Clean that up...
                boardContainer.CleanContainer();
            }

            var partContainer = ContainerManagerComponent.Ensure<Container>(MachineFrameComponent.PartContainer, machine, out existed);

            if (existed)
            {
                // Clean that up, too...
                partContainer.CleanContainer();
            }

            boardContainer.Insert(board);

            // Now we insert all parts.
            foreach (var part in entPartContainer.ContainedEntities.ToArray())
            {
                entPartContainer.ForceRemove(part);
                partContainer.Insert(part);
            }

            if (machine.TryGetComponent(out ConstructionComponent? construction))
            {
                // We only add these two container. If some construction needs to take other containers into account, fix this.
                construction.AddContainer(MachineFrameComponent.BoardContainer);
                construction.AddContainer(MachineFrameComponent.PartContainer);
            }

            if (machine.TryGetComponent(out MachineComponent? machineComp))
            {
                machineComp.RefreshParts();
            }

            entity.Delete();
        }

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
        }
    }
}
