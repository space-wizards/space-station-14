#nullable enable
using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
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
