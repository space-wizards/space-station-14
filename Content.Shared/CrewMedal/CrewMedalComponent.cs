using Robust.Shared.GameStates;

namespace Content.Shared.CrewMedal;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CrewMedalComponent : Component
{
    [AutoNetworkedField]
    [DataField]
    public string Recipient = "";

    [AutoNetworkedField]
    [DataField]
    public string Reason = "";

    [AutoNetworkedField]
    [DataField]
    public bool Awarded = false;

    [AutoNetworkedField]
    [DataField]
    public int MaxCharacters = 50;
}
