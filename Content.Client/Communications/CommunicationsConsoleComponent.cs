using Content.Shared.Communications;
using Robust.Shared.Prototypes;

namespace Content.Client.Communications;

[RegisterComponent]
public sealed partial class CommunicationsConsoleComponent : SharedCommunicationsConsoleComponent
{
    /// <summary>
    ///     The prototype ID to use in the UI to show what entities a broadcast will display on
    /// </summary>
    [DataField]
    public EntProtoId ScreenDisplayId = "Screen";
}
