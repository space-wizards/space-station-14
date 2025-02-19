using Content.Shared.Roles;
using Robust.Shared.GameStates;

namespace Content.Shared.Changeling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChangelingIdentityComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntityUid> ConsumedIdentities = [];

    [DataField, AutoNetworkedField]
    public EntityUid? LastConsumedEntityUid;

}
