using Robust.Shared.GameStates;

namespace Content.Shared.Changeling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChangelingIdentityComponent : Component
{
    //TODO: Figure out if it's better to have a list of 1 identity at the start (the original Identity) or have a separate one as a fallback
    [DataField, AutoNetworkedField]
    public List<EntityUid> ConsumedIdentities = []; // Nullspaced Identities

    [DataField, AutoNetworkedField]
    public EntityUid? LastConsumedEntityUid; // nullspaced identity

}
