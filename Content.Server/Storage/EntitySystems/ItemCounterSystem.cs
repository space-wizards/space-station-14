using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Storage.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server.Storage.EntitySystems
{
    [UsedImplicitly]
    public class ItemCounterSystem : SharedItemCounterSystem
    {
        protected override bool TryGetLayers(ContainerModifiedMessage msg,
            ItemMapperComponent itemMapper,
            out IReadOnlyList<string> showLayers)
        {
            if (msg.Container.Owner.TryGetComponent(out ServerStorageComponent? component))
            {
                var containedLayers = component.StoredEntities ?? new List<IEntity>();
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

        protected override bool TryGetCount(ContainerModifiedMessage msg, ItemCounterComponent itemCounter,
            [NotNullWhen(true)] out int? count)
        {
            if (!msg.Container.Owner.TryGetComponent(out ServerStorageComponent? component)
                || component.StoredEntities == null)
            {
                count = null;
                return false;

            }

            var c = 0;
            foreach (var entity in component.StoredEntities)
            {
                if (itemCounter.Count.IsValid(entity)) c++;
            }

            count = c;
            return true;
        }
    }
}
