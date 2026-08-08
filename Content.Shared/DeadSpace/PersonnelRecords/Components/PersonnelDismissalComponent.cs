// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.PersonnelRecords.Components;

/// <summary>
/// Marker added to <c>ComputerId</c> in yaml (upstream edit, in DS14 markers) that grants it the
/// "Уволить" (Dismiss) button. The client shows the button purely by <c>HasComp</c> on this
/// component - deliberately not by touching <c>IdCardConsoleBoundUserInterfaceState</c>, so the
/// upstream state class stays untouched.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PersonnelDismissalComponent : Component
{
}
