using Robust.Shared.GameStates;

namespace Content.Shared.UserInterface;

/// <summary>
/// Component for multiple verb-only user interfaces on one entity
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MultiActivatableUIComponent : Component
{
    /// <summary>
    /// List of user interface keys used to open correct UI features and their respective verb text
    /// </summary>
    public Dictionary<Enum, LocId> KeyVerbs = [];
}
