// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.CriminalRecords.Systems;
using Content.Shared.DeadSpace.PersonnelRecords;
using Content.Shared.DeadSpace.PersonnelRecords.Systems;

namespace Content.Server.DeadSpace.PersonnelRecords.Systems;

/// <summary>
/// The one-way bridge from Personnel Records into Criminal Records (§ "Связь с СБ").
///
/// Personnel and criminal status are two independent axes - reusing <c>SecurityStatus</c> for
/// employment status was explicitly rejected in the design (clearing a wanted flag would silently
/// erase an HR order, and Security couldn't tell a thief from someone merely marked for
/// dismissal). <c>PersonnelRecordsSystem</c> never calls into <c>CriminalRecordsSystem</c>
/// directly; this system is the only thing that listens for <see cref="PersonnelStatusChangedEvent"/>
/// and writes a context line into the target's <c>CriminalRecord.History</c>, purely so Security's
/// own console shows *why* someone might be worth stopping - it never touches
/// <c>CriminalRecord.Status</c>.
///
/// Note on radio: the Security-channel radio announcements for these same transitions are already
/// sent by <c>PersonnelRecordsConsoleSystem</c> (§ "Валидация, логи, рация" spells out the exact
/// texts). This system deliberately does not send a second one - doing so per §2.8's phrasing
/// literally would double every Security announcement for the same event.
/// </summary>
public sealed class PersonnelSecurityBridgeSystem : EntitySystem
{
    [Dependency] private readonly CriminalRecordsSystem _criminalRecords = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PersonnelStatusChangedEvent>(OnPersonnelStatusChanged);
    }

    private void OnPersonnelStatusChanged(ref PersonnelStatusChangedEvent ev)
    {
        var locKey = ev.ActionType switch
        {
            PersonnelActionType.Demotion => "personnel-records-criminal-history-demotion",
            PersonnelActionType.Dismissal => "personnel-records-criminal-history-dismissal",
            PersonnelActionType.Annul => "personnel-records-criminal-history-annulled",
            PersonnelActionType.Executed => "personnel-records-criminal-history-executed",
            // A bare reprimand is a department matter only and never reaches Criminal Records.
            _ => null,
        };

        if (locKey is null)
            return;

        var text = Loc.GetString(locKey, ("reason", ev.Reason ?? string.Empty));
        _criminalRecords.TryAddHistory(ev.Key, text, ev.InitiatorName);
    }
}
