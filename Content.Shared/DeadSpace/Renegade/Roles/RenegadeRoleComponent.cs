// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Roles.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Renegade.Roles;

[RegisterComponent, NetworkedComponent]
public sealed partial class RenegadeRoleComponent : BaseMindRoleComponent;
