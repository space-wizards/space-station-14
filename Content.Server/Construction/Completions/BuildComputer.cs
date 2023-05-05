using System.Linq;
using Content.Server.Construction.Components;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class BuildComputer : IGraphAction
    {
        [DataField("container")] public string Container { get; private set; } = string.Empty;

        // TODO use or generalize ConstructionSystem.ChangeEntity();
        // TODO pass in node/edge & graph ID for better error logs.
        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (!entityManager.TryGetComponent(uid, out ContainerManagerComponent? containerManager))
            {
                Logger.Error($"Computer entity {entityManager.ToPrettyString(uid)} did not have a container manager! Aborting build computer action.");
                return;
            }

            var containerSystem = entityManager.EntitySysManager.GetEntitySystem<ContainerSystem>();

            if (!containerSystem.TryGetContainer(uid, Container, out var container, containerManager))
            {
                Logger.Error($"Computer entity {entityManager.ToPrettyString(uid)} did not have the specified '{Container}' container! Aborting build computer action.");
                return;
            }

            if (container.ContainedEntities.Count != 1)
            {
                Logger.Error($"Computer entity {entityManager.ToPrettyString(uid)} did not have exactly one item in the specified '{Container}' container! Aborting build computer action.");
                return;
            }

            var board = container.ContainedEntities[0];

            if (!entityManager.TryGetComponent(board, out ComputerBoardComponent? boardComponent))
            {
                Logger.Error($"Computer entity {entityManager.ToPrettyString(uid)} had an invalid entity in container \"{Container}\"! Aborting build computer action.");
                return;
            }

            container.Remove(board);

            var transform = entityManager.GetComponent<TransformComponent>(uid);
            var computer = entityManager.SpawnEntity(boardComponent.Prototype, transform.Coordinates);
            entityManager.GetComponent<TransformComponent>(computer).LocalRotation = transform.LocalRotation;

            var computerContainer = containerSystem.EnsureContainer<Container>(computer, Container);

            // In case it already existed and there are any entities inside the container, delete them.
            foreach (var ent in computerContainer.ContainedEntities.ToArray())
            {
                computerContainer.ForceRemove(ent);
                entityManager.DeleteEntity(ent);
            }

            computerContainer.Insert(board);

            // We only add this container. If some construction needs to take other containers into account, fix this.
            entityManager.EntitySysManager.GetEntitySystem<ConstructionSystem>().AddContainer(computer, Container);

            var entChangeEv = new ConstructionChangeEntityEvent(computer, uid);
            entityManager.EventBus.RaiseLocalEvent(uid, entChangeEv);
            entityManager.EventBus.RaiseLocalEvent(computer, entChangeEv, broadcast: true);

            // Delete the original entity.
            entityManager.QueueDeleteEntity(uid);
        }
    }
}
