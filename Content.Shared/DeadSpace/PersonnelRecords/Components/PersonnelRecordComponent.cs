// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.PersonnelRecords.Systems;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.PersonnelRecords.Components;

/// <summary>
/// Marks a mob as currently subject to an active Demotion or Dismissal order, driving the HUD
/// icon shown by <c>ShowPersonnelRecordIconsSystem</c>. Mirrors <c>CriminalRecordComponent</c>
/// exactly - added/removed by <see cref="SharedPersonnelRecordsSystem.SetPersonnelIcon"/>, never
/// present for a bare Reprimand or a clean record.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedPersonnelRecordsSystem))]
public sealed partial class PersonnelRecordComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<SecurityIconPrototype> StatusIcon = "PersonnelIconDismissal";
}
