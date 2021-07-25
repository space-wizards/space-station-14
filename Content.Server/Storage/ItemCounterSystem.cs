using System.Collections.Generic;
using Content.Server.Storage.Components;
using Content.Shared.Storage.ItemCounter;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server.Storage
{
    [UsedImplicitly]
    public class ItemCounterSystem : SharedItemCounterSystem
    {
        protected override bool TryGetContainer(ContainerModifiedMessage msg,
            ItemCounterComponent itemCounter,
            out IReadOnlyList<string> showLayers)
        {
            if (msg.Container.Owner.TryGetComponent(out ServerStorageComponent? component))
            {
                var containedLayers = component.StoredEntities ?? new List<IEntity>();
                var list = new List<string>();
                foreach (var mapLayerData in itemCounter.MapLayers.Values)
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
