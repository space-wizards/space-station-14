#nullable enable
using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Containers;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    public class EmptyAllContainers : IGraphAction
    {
        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
        }

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted || !entity.TryGetComponent<ContainerManagerComponent>(out var containerManager))
                return;

            foreach (var container in containerManager.GetAllContainers())
            {
                container.EmptyContainer(true, entity.Transform.Coordinates);
            }
        }
    }
}
