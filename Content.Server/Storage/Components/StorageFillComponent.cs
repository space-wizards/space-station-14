using Content.Server.Storage.EntitySystems;
using Content.Shared.Storage;

namespace Content.Server.Storage.Components
{
    [RegisterComponent, Access(typeof(StorageSystem))]
    public sealed partial class StorageFillComponent : Component
    {
        [DataField("contents")] public List<EntitySpawnEntry> Contents = new();
    }
}
