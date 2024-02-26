using Content.Shared.EntityList;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Storage.Components;

// TODO:
// REPLACE THIS WITH CONTAINERFILL
[RegisterComponent, NetworkedComponent, Access(typeof(SharedStorageSystem))]
public sealed partial class StorageFillComponent : Component
{
    [DataField("contents")] public List<EntitySpawnEntry> Contents = new();
    [DataField("overrides")] public List<ProtoId<EntityOverridePrototype>> Overrides = new();
}
