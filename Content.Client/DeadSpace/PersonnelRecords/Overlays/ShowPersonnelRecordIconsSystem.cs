// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Client.Overlays;
using Content.Shared.DeadSpace.PersonnelRecords.Components;
using Content.Shared.DeadSpace.PersonnelRecords.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.DeadSpace.PersonnelRecords.Overlays;

/// <summary>
/// Mirrors <c>Content.Client.Overlays.ShowCriminalRecordIconsSystem</c> exactly, for the
/// Personnel Records HUD layer instead of the criminal one.
/// </summary>
public sealed class ShowPersonnelRecordIconsSystem : EquipmentHudSystem<ShowPersonnelRecordIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PersonnelRecordComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, PersonnelRecordComponent component, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        if (_prototype.Resolve(component.StatusIcon, out var iconPrototype))
            ev.StatusIcons.Add(iconPrototype);
    }
}
