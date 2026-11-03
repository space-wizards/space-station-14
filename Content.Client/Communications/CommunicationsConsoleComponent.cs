using Content.Shared.Communications;
using Robust.Shared.Prototypes;

namespace Content.Client.Communications;

/// <inheritdoc/>
[RegisterComponent]
public sealed partial class CommunicationsConsoleComponent : SharedCommunicationsConsoleComponent
{
    /// <summary>
    /// The prototype ID to use in the UI to show what entities a broadcast will display on.
    /// </summary>
    /// <remarks>
    /// The UI works expecting a screen that's roughly 32x16 pixels, centered in a 32x32 box.
    /// If this isn't true, create a dummy entity or adjust the margins/scale in MessagingControls.xaml.
    /// </remarks>
    [DataField]
    public EntProtoId ScreenDisplayId = "Screen";
}
