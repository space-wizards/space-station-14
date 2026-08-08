// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.PersonnelRecords.Overlays;

/// <summary>
/// Marker component granting HUD visibility of <c>PersonnelRecordComponent</c> icons, mirroring
/// <c>ShowCriminalRecordIconsComponent</c>. Worn equipment (security/head-of-department eyewear)
/// carries this via <c>Content.Client.DeadSpace.PersonnelRecords.Overlays.ShowPersonnelRecordIconsSystem</c>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowPersonnelRecordIconsComponent : Component
{
}
