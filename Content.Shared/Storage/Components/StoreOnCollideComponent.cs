using Content.Shared.Storage.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(StoreOnCollideSystem))]
public sealed partial class StoreOnCollideComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool BodyOnly;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool LockOnCollide;
}
