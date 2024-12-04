using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

// Use where you want an entity to store other entities on collide
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(StoreOnCollideSystem))]
public sealed partial class StoreOnCollideComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool LockOnCollide;

    [DataField]
    public bool DisableWhenFirstOpened;

    [DataField, AutoNetworkedField]
    public bool Disabled;
}
