// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.DeadSpace.PersonnelRecords;
using Content.Shared.DeadSpace.PersonnelRecords.Components;
using Content.Shared.DeadSpace.Photocopier;
using Content.Shared.Paper;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.StationRecords;
using Robust.Shared.Audio.Systems;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.PersonnelRecords.Systems;

/// <summary>
/// Handles the "Распечатать приказ" button: reads the discipline order template, fills in the
/// same four base placeholders <c>PhotocopierSystem.PrintForm</c> uses (via the shared
/// <see cref="PaperworkTextSubstitutions"/>) plus its own author/target/sanction/reason set, and
/// spawns the paper directly at the console - no photocopier involved (§2.2).
///
/// Printing is visibility-only (<see cref="PersonnelRecordsConsoleSystem.CanView"/>): the order
/// already exists, the paper is just its physical copy, so there's no self/protected-department
/// restriction here unlike issuing/annulling an order.
/// </summary>
public sealed class PersonnelPrintingSystem : EntitySystem
{
    private const string DismissedJobId = "Dismissed";

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PersonnelRecordsConsoleSystem _console = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedJobSystem _jobSystem = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Own (component, event) pair on the same enum.PersonnelRecordsConsoleKey.Key that
        // PersonnelRecordsConsoleSystem already subscribes for other messages - no collision, same
        // reasoning as the dismiss button on the ID card console (see PersonnelDismissalSystem).
        Subs.BuiEvents<PersonnelRecordsConsoleComponent>(PersonnelRecordsConsoleKey.Key, subs =>
        {
            subs.Event<PersonnelRecordPrintOrder>(OnPrintOrder);
        });
    }

    private void OnPrintOrder(Entity<PersonnelRecordsConsoleComponent> ent, ref PersonnelRecordPrintOrder msg)
    {
        if (!_console.TryCheckSelected(ent, msg.Actor, out var mob, out var key))
            return;

        if (_timing.CurTime < ent.Comp.NextPrintTime)
            return;

        if (!_records.TryGetRecord<GeneralStationRecord>(key.Value, out var general))
            return;

        if (!_console.CanView(mob.Value, ent.Comp, general))
            return;

        // Prints the currently active order only - Status == None means nothing to print, and
        // there is deliberately no way to print a past, already-closed order after the fact
        // (§2.2: PersonnelHistory doesn't carry the department/job needed to do it honestly).
        if (!_records.TryGetRecord<PersonnelRecord>(key.Value, out var record) || record.Status == EmploymentStatus.None)
            return;

        PrintOrder(ent, mob.Value, general, record);
    }

    private void PrintOrder(Entity<PersonnelRecordsConsoleComponent> ent, EntityUid actor, GeneralStationRecord general, PersonnelRecord record)
    {
        var sanctionKey = record.Status switch
        {
            EmploymentStatus.Reprimand => "personnel-records-print-sanction-reprimand",
            EmploymentStatus.Demotion => "personnel-records-print-sanction-demotion",
            EmploymentStatus.Dismissal => "personnel-records-print-sanction-dismissal",
            _ => null,
        };

        if (sanctionKey is null)
            return;

        if (!_prototype.TryIndex(ent.Comp.OrderForm, out var formPrototype))
            return;

        var text = _resourceManager.ContentFileReadText(formPrototype.Text).ReadToEnd();

        var stationName = _station.GetOwningStation(ent) is { } station ? Name(station) : null;
        text = PaperworkTextSubstitutions.ApplyBase(text, Loc.GetString(formPrototype.Name), _gameTicker.RoundDuration(), stationName);

        // Deliberately NOT GetOfficer/TryGetIdentityShortInfoEvent here - that returns
        // "FullName (JobTitle)" as one combined string (fine for the "Ответственный: {officer}"
        // radio line, which already reads naturally with a job in parens attached to a name).
        // {{AUTHOR.NAME}} and {{AUTHOR.JOB}} are two separate template fields, so pulling the
        // combined string into AUTHOR.NAME produced "Фома Быков (Глава Персонала)" there and then
        // the template's own "в должности {{AUTHOR.JOB}}" duplicated the job right after it.
        var authorName = Loc.GetString("personnel-records-console-unknown-officer");
        var authorJob = string.Empty;
        if (_idCard.TryFindIdCard(actor, out var authorCard))
        {
            if (!string.IsNullOrWhiteSpace(authorCard.Comp.FullName))
                authorName = authorCard.Comp.FullName;
            authorJob = authorCard.Comp.LocalizedJobTitle ?? string.Empty;
        }

        var department = _jobSystem.TryGetPrimaryDepartment(general.JobPrototype, out var dept)
            ? Loc.GetString(dept.Name)
            : Loc.GetString("personnel-records-print-unknown-department");

        var newJob = record.Status switch
        {
            // A reprimand doesn't change the job at all.
            EmploymentStatus.Reprimand => general.JobTitle,
            // The head fills the new job in by hand once they've actually reassigned it.
            EmploymentStatus.Demotion => string.Empty,
            EmploymentStatus.Dismissal => _prototype.TryIndex<JobPrototype>(DismissedJobId, out var dismissedJob) ? dismissedJob.LocalizedName : string.Empty,
            _ => string.Empty,
        };

        text = text.Replace("{{AUTHOR.NAME}}", authorName);
        text = text.Replace("{{AUTHOR.JOB}}", authorJob);
        text = text.Replace("{{TARGET.NAME}}", general.Name);
        text = text.Replace("{{TARGET.DEPARTMENT}}", department);
        text = text.Replace("{{SANCTION}}", Loc.GetString(sanctionKey));
        text = text.Replace("{{TARGET.NEWJOB}}", newJob);
        text = text.Replace("{{REASON}}", record.Reason ?? string.Empty);

        var printed = Spawn(formPrototype.PaperPrototype, Transform(ent).Coordinates);
        if (TryComp<PaperComponent>(printed, out var paper))
            _paperSystem.SetContent((printed, paper), text);

        _audio.PlayPvs(ent.Comp.PrintSound, ent);
        _console.SetNextPrintTime(ent.Comp, _timing.CurTime + ent.Comp.PrintDelay);
    }
}
