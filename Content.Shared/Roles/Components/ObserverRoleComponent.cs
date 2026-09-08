using Robust.Shared.GameStates;

namespace Content.Shared.Roles.Components;

/// <summary>
/// This is used to mark Observers properly, as they get Minds.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ObserverRoleComponent : BaseMindRoleComponent;
