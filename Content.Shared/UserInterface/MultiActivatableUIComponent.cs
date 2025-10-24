using Robust.Shared.GameStates;

namespace Content.Shared.UserInterface;

/// <summary>
/// Component for multiple verb-only user interfaces on one entity
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MultiActivatableUIComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<Enum> Keys = [];

    [DataField, AutoNetworkedField]
    public List<LocId> VerbTexts = [];
}
