// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.PersonnelRecords.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.PersonnelRecords.Systems;

/// <summary>
/// Shared base for the Personnel Records record-mutation system, mirroring
/// <c>Content.Shared.CriminalRecords.Systems.SharedCriminalRecordsSystem</c>.
///
/// Record creation and status-transition logic (issue/annul/execute) live in
/// <c>Content.Server.DeadSpace.PersonnelRecords.Systems.PersonnelRecordsSystem</c>, added
/// incrementally across Phases 1, 2 and 5. This shared base only owns the HUD identity/icon sync
/// that has to run on both sides, exactly like its criminal-records counterpart.
/// </summary>
public abstract class SharedPersonnelRecordsSystem : EntitySystem
{
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("personnel-records");
    }

    /// <summary>
    /// Any entity whose current visible name matches the record that was just changed gets its
    /// icon updated (or removed, for None/Reprimand - reprimands never get a HUD icon).
    ///
    /// DS14-diagnostic: logs whether a match was found and, if not, the closest names that were
    /// actually seen while scanning - temporary instrumentation for tracking down why the icon
    /// sometimes never gets attached through this path (remove once confirmed fixed).
    /// </summary>
    public void UpdatePersonnelIdentity(string name, EmploymentStatus status)
    {
        var query = EntityQueryEnumerator<IdentityComponent>();
        var matched = false;

        while (query.MoveNext(out var uid, out _))
        {
            var visibleName = Identity.Name(uid, EntityManager);
            if (!visibleName.Equals(name))
                continue;

            matched = true;
            _sawmill.Info($"UpdatePersonnelIdentity: matched {ToPrettyString(uid)} (visible name '{visibleName}') against record name '{name}', status={status}");

            if (status is EmploymentStatus.Demotion or EmploymentStatus.Dismissal)
                SetPersonnelIcon(status, uid);
            else
                RemComp<PersonnelRecordComponent>(uid);
        }

        if (!matched)
            _sawmill.Warning($"UpdatePersonnelIdentity: no IdentityComponent entity matched record name '{name}' (status={status}) - icon was NOT attached to anyone");
    }

    /// <summary>
    /// Decides the icon that should be displayed on the entity based on the employment status.
    /// Only Demotion/Dismissal carry an icon - a bare Reprimand is deliberately invisible on the
    /// HUD (§ "У выговора иконки HUD нет").
    /// </summary>
    public void SetPersonnelIcon(EmploymentStatus status, EntityUid characterUid)
    {
        EnsureComp<PersonnelRecordComponent>(characterUid, out var record);

        var previousIcon = record.StatusIcon;

        record.StatusIcon = status switch
        {
            EmploymentStatus.Demotion => "PersonnelIconDemotion",
            EmploymentStatus.Dismissal => "PersonnelIconDismissal",
            _ => record.StatusIcon,
        };

        if (previousIcon != record.StatusIcon)
            Dirty(characterUid, record);
    }
}

/// <summary>
/// Raised whenever a <see cref="PersonnelRecord"/>'s status changes for any reason (order issued,
/// annulled, or - from Phase 5 - executed). <see cref="PersonnelSecurityBridgeSystem"/> is the only
/// subscriber that matters here: <c>PersonnelRecordsSystem</c> never calls into Criminal Records
/// directly, keeping the two record types on genuinely independent axes (§ "Связь с СБ").
/// </summary>
[ByRefEvent]
public record struct PersonnelStatusChangedEvent(StationRecordKey Key, PersonnelActionType ActionType, EmploymentStatus NewStatus, string? Reason, string? InitiatorName);

/// <summary>
/// Raised by <c>PersonnelRecordsSystem.TryExecuteOrder</c> once a Demotion/Dismissal order has
/// been carried out (the target's job no longer matches <see cref="PersonnelRecord.JobAtOrder"/>
/// at the moment execution was detected). <c>PersonnelVacancySystem</c> is the sole subscriber -
/// deliberately kept separate from the completion detector itself (§2.4: "Архитектурно прибавка
/// слота живёт не внутри детектора исполнения, а в отдельной PersonnelVacancySystem").
/// </summary>
[ByRefEvent]
public record struct PersonnelOrderExecutedEvent(StationRecordKey Key, ProtoId<JobPrototype> ExecutedJob, EmploymentStatus PreviousStatus);
