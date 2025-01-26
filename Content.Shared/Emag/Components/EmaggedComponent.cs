using Content.Shared.Emag.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Emag.Components;

/// <summary>
/// Marker component for emagged entities
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmaggedComponent : Component
{

    [DataField, AutoNetworkedField]
    public EmagType EmagType = EmagType.None;
}
