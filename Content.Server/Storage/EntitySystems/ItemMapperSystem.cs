using System.Collections.Generic;
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
        protected override bool TryGetLayers(ContainerModifiedMessage msg,
            ItemMapperComponent itemMapper,
            out IReadOnlyList<string> showLayers)
        {
            if (EntityManager.TryGetComponent(msg.Container.Owner, out ServerStorageComponent? component))
            {
                var containedLayers = component.StoredEntities ?? new List<EntityUid>();
                var list = new List<string>();
                foreach (var mapLayerData in itemMapper.MapLayers.Values)
                {
                    foreach (var entity in containedLayers)
                    {
                        if (mapLayerData.Whitelist.IsValid(entity))
                        {
                            list.Add(mapLayerData.Layer);
                            break;
                        }
                    }
                }

                showLayers = list;
                return true;
            }

            showLayers = new List<string>();
            return false;
        }
    }
}
