using Content.Shared.Storage.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

// TODO:
// REPLACE THIS WITH CONTAINERFILL
[RegisterComponent, NetworkedComponent, Access(typeof(SharedStorageSystem))]
public sealed partial class StorageFillComponent : Component
{
    [DataField("contents")] public List<EntitySpawnEntry> Contents = new();
}
