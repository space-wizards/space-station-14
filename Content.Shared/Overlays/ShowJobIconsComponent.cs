using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

/// <summary>
///     This component allows you to see job icons above mobs.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShowJobIconsComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IncludeCrewBorder = false;
}
