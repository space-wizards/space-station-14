using Content.Shared.Overlays;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.StatusIcon.Components;

/// <summary>
/// Used to indicate a mob can have their job status read by HUDs.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JobStatusComponent : Component
{
    /// <summary>
    /// The currently displayed status icon for the mobs's job.
    /// Visible with <see cref="ShowJobIconsComponent"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<JobIconPrototype>? JobStatusIcon = "JobIconNoId";

    /// <summary>
    /// If the mob is currently considered crew.
    /// This is true depending on their current job icon.
    /// Visible with <see cref="ShowCrewIconsComponent"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsCrew;
}
