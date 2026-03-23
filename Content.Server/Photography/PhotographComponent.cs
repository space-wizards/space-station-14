using Robust.Shared.Utility;

namespace Content.Server.Photography;
/// <summary>
///  Represents the photograph data on an picture
/// </summary>
[RegisterComponent]

public sealed partial class PhotographComponent : Component
{
    /// <summary>
    /// The description of the photographed object
    /// </summary>
    [DataField, AutoNetworkedField]
    public FormattedMessage Text;

    /// <summary>
    ///  The name of the photographed object
    /// </summary>

    [DataField, AutoNetworkedField]
    public string Name;
    public PhotographComponent(string name, FormattedMessage descText)
    {
        Text = descText;
        Name = name;
    }
}
