using Content.Shared.Storage.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

// Don't remove before December 2026.
// A lot of forks still use this for their prototypes.
[Obsolete("Use ContainerFillComponent or EntityTableContainerFillComponent instead")]
[RegisterComponent, NetworkedComponent, Access(typeof(SharedStorageSystem))]
public sealed partial class StorageFillComponent : Component
{
    [DataField("contents")] public List<EntitySpawnEntry> Contents = new();
}
