using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

[NetworkedComponent]
public abstract class SharedStorageFillComponent : Component
{
    [DataField("contents")] public List<EntitySpawnEntry> Contents = new();
}
