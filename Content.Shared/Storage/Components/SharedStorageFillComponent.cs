using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

[NetworkedComponent]
[RegisterComponent]
public class StorageFillComponent : Component
{
    [DataField("contents")] public List<EntitySpawnEntry> Contents = new();
}
