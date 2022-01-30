using System.Collections.Generic;
using Content.Server.Storage.EntitySystems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Storage.Components
{
    [RegisterComponent, ComponentProtoName("StorageFill"), Friend(typeof(StorageSystem))]
    public sealed class StorageFillComponent : Component
    {
        [DataField("contents")] public List<EntitySpawnEntry> Contents = new();
    }
}
