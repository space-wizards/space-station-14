using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

/// <summary>
///     This component allows you to see job icons above mobs.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class ShowJobIconsComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IncludeCrewBorder = false;

    [DataField, AutoNetworkedField]
    public bool UncertainCrewBorder = false;
}
