using System.Collections.Generic;
using System.Linq;
using Content.Server.Storage.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Storage.EntitySystems
{
    [UsedImplicitly]
    public sealed class ItemMapperSystem : SharedItemMapperSystem
    {
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

        protected override bool TryGetLayers(ContainerModifiedMessage msg,
            ItemMapperComponent itemMapper,
            out IReadOnlyList<string> showLayers)
        {
            var containedLayers = _containerSystem.GetAllContainers(msg.Container.Owner)
                .SelectMany(cont => cont.ContainedEntities).ToArray();

            var list = new List<string>();
            foreach (var mapLayerData in itemMapper.MapLayers.Values)
            {
                var count = containedLayers.Count(uid => mapLayerData.ServerWhitelist.IsValid(uid));
                if (count >= mapLayerData.MinCount && count <= mapLayerData.MaxCount)
                {
                    list.Add(mapLayerData.Layer);
                }
            }

            showLayers = list;
            return true;
        }
    }
}
