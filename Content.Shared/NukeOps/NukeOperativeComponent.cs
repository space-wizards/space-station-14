using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.NukeOps;

/// <summary>
/// This is used for tagging a mob as a nuke operative.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NukeOperativeComponent : Component
{
    /// <summary>
    /// The icon representing the nuclear operative's faction. Visible to other nuclear operatives.
    /// </summary>
    [DataField]
    public ProtoId<StatusIconPrototype> SyndStatusIcon = "SyndicateFaction";
}
