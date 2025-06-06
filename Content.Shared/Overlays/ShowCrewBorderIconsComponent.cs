using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

/// <summary>
///     This component allows you to see a crew border icon above mobs.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class ShowCrewBorderIconsComponent : Component
{
    /// <summary>
    /// If true, the HUD will include a green border around jobs that are considered crew according to <see cref="JobIconPrototype.IsCrewJob"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IncludeCrewBorder = false;

    /// <summary>
    /// If true while <see cref="IncludeCrewBorder"/> is true, the HUD will include a yellow border around all icons, to indicate crew uncertainty.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UncertainCrewBorder = false;
}
