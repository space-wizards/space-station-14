using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Research.Components;

/// <summary>
///     Console used to unlock research by spending R&D points.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ResearchConsoleComponent : Component
{
    /// <summary>
    /// The radio channel that the unlock announcements are broadcast to.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> AnnouncementChannel = "Science";
}

