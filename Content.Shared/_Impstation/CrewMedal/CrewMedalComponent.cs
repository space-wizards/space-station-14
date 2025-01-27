using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.CrewMedal;

/// <summary>
///    Makes a medal recipent show up on the round end screen.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CrewMedalComponent : Component
{
    /// <summary>
    ///    Name of the person receiving the award.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public string Recipient = "";

    /// <summary>
    ///    Reason for the award. Can be set using an interface.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public string Reason = "";

    /// <summary>
    ///    Has the medal been awarded?
    ///    If this is true the recipient and reason can no longer be changed.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public bool Awarded = false;

    /// <summary>
    ///    Max character limit for the reason string.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public int MaxCharacters = 50;
}
