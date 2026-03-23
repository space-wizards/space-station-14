using Robust.Shared.Utility;

namespace Content.Shared.Photography;
/// <summary>
///  Represents the photograph data on an picture
/// </summary>
[RegisterComponent]
[AutoGenerateComponentState]
public sealed partial class PhotographComponent : Component
{
    /// <summary>
    /// The description of the photographed object
    /// </summary>
    [DataField, AutoNetworkedField]
    public FormattedMessage Text = default!;

    /// <summary>
    ///  The name of the photographed object
    /// </summary>

    [DataField, AutoNetworkedField]
    public string Name = default!;
}
