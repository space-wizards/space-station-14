using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Photography;

/// <summary>
/// Represents the photograph data on a picture.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PhotographComponent : Component
{
    /// <summary>
    /// The description of the photographed object.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FormattedMessage? Description;

    /// <summary>
    /// The full text mentioning the name of the photographed object.
    /// For example "This is a picture of Urist McHands"
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? NameText;
}
