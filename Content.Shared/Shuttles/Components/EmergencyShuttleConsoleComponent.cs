using Robust.Shared.GameStates;

namespace Content.Shared.Shuttles.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class EmergencyShuttleConsoleComponent : Component
{
    /// <summary>
    /// ID cards that have been used to authorize an early launch.
    /// Key is the UID of the ID card,
    /// value is the card's name at the time of authorization.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("authorized")]
    public Dictionary<EntityUid, string> AuthorizedEntities = new();

    [ViewVariables(VVAccess.ReadWrite), DataField("authorizationsRequired")]
    public int AuthorizationsRequired = 3;
}
