#nullable enable
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Construction;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    public class BuildComputer : IGraphAction
    {
        public string Container { get; private set; } = string.Empty;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Container, "container", string.Empty);
        }

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (!entity.TryGetComponent(out ContainerManagerComponent? containerManager)) return;

            if (!containerManager.TryGetContainer(Container, out var container)) return;

            var board = container.ContainedEntities[0];

            if (!board.TryGetComponent(out ComputerBoardComponent? boardComponent)) return;

            var entityManager = entity.EntityManager;
            container.Remove(board);

            var computer = entityManager.SpawnEntity(boardComponent.Prototype, entity.Transform.Coordinates);

            var computerContainer = ContainerManagerComponent.Ensure<Container>(Container, computer);
            computerContainer.Insert(board);
            entity.Delete();
        }
    }
}
