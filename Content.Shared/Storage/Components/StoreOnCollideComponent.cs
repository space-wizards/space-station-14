using Content.Shared.Storage.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

// Use where you want an entity to store other entities on collide
[RegisterComponent, NetworkedComponent, Access(typeof(StoreOnCollideSystem))]
public sealed partial class StoreOnCollideComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool BodyOnly;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool LockOnCollide;
}
