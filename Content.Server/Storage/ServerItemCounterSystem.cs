using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Storage.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.ItemCounter;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server.Storage
{
    [UsedImplicitly]
    public class ServerItemCounterSystem : ItemCounterSystem
    {
        protected override bool TryGetContainer(ContainerModifiedMessage msg,
            [NotNullWhen(true)] out IEntity? containerEntity,
            out IReadOnlyList<EntityUid> containedEntities)
        {
            if (msg.Container.Owner.TryGetComponent(out ServerStorageComponent? component))
            {
                containerEntity = component.Owner;

                var entities = component.StoredEntities ?? new List<IEntity>();
                var uids = new List<EntityUid>(entities.Count);
                foreach (var ent in entities)
                {
                    uids.Add(ent.Uid);
                }

                containedEntities = uids;
                return true;
            }

            containerEntity = null;
            containedEntities = new List<EntityUid>();
            return false;
        }
    }
}