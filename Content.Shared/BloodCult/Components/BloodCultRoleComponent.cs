using Content.Shared.Roles.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.BloodCult.Components;

/// <summary>
/// Role component for Blood Cult antagonist.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodCultRoleComponent : BaseMindRoleComponent;
