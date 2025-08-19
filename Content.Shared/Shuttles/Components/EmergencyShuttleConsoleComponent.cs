using Robust.Shared.GameStates;

namespace Content.Shared.Shuttles.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class EmergencyShuttleConsoleComponent : Component
{
    // TODO: Okay doing it by string is kinda suss but also ID card tracking doesn't seem to be robust enough

    /// <summary>
    /// ID cards that have been used to authorize an early launch.
    /// </summary>
    [DataField("authorized"), AutoNetworkedField]
    public HashSet<string> AuthorizedEntities = [];

    [DataField, AutoNetworkedField]
    public int AuthorizationsRequired = 3;
}
