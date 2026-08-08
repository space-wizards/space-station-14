// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Access.Systems;
using Content.Server.Popups;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DeadSpace.PersonnelRecords;
using Content.Shared.DeadSpace.PersonnelRecords.Components;
using Content.Shared.Roles;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;
using static Content.Shared.Access.Components.IdCardConsoleComponent;

namespace Content.Server.DeadSpace.PersonnelRecords.Systems;

/// <summary>
/// Handles the "Уволить" (Dismiss) button added to the ID card console. Subscribed via
/// <c>Subs.BuiEvents&lt;PersonnelDismissalComponent&gt;</c> on the same
/// <c>enum.IdCardConsoleUiKey.Key</c> that <c>IdCardConsoleSystem</c> already uses - a distinct
/// component and a distinct message type mean the two subscriptions never collide (verified by
/// reading <c>BoundUserInterfaceRegisterExt</c>: <c>Subs.BuiEvents</c> is a plain
/// <c>SubscribeLocalEvent&lt;TComp, TEvent&gt;</c> with an extra <c>UiKey</c> filter).
///
/// Deliberately does not touch <c>IdCardConsoleSystem</c> itself: <c>PrivilegedIdIsAuthorized</c>
/// and the general-record sync it needs are both private there, and small enough to reimplement
/// here rather than widen an upstream method's visibility for a six-line check.
/// </summary>
public sealed class PersonnelDismissalSystem : EntitySystem
{
    private const string DismissedJobId = "Dismissed";

    [Dependency] private readonly AccessSystem _access = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<PersonnelDismissalComponent>(IdCardConsoleUiKey.Key, subs =>
        {
            subs.Event<PersonnelDismissMessage>(OnDismiss);
        });
    }

    private void OnDismiss(Entity<PersonnelDismissalComponent> ent, ref PersonnelDismissMessage msg)
    {
        // The ID card console sits on BaseComputerAiAccess so the AI/borgs can open it, but
        // PrivilegedIdIsAuthorized below only checks whatever card happens to be sitting in the
        // console's privileged slot - not who's actually pressing the button. Without this, any
        // silicon could walk up to a console someone left a valid ID in and dismiss anyone, same gap
        // already closed on the Personnel Records console itself (see PersonnelRecordsConsoleSystem.
        // CheckSelected/CanAct).
        if (HasComp<StationAiHeldComponent>(msg.Actor) || HasComp<BorgChassisComponent>(msg.Actor))
            return;

        if (!TryComp<IdCardConsoleComponent>(ent, out var console))
            return;

        if (!PrivilegedIdIsAuthorized(ent, console))
        {
            _popup.PopupEntity(Loc.GetString("personnel-dismissal-permission-denied"), ent, msg.Actor);
            return;
        }

        if (console.TargetIdSlot.Item is not { Valid: true } targetId)
            return;

        if (!TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage) || keyStorage.Key is not { } key)
        {
            _popup.PopupEntity(Loc.GetString("personnel-dismissal-no-record"), ent, msg.Actor);
            return;
        }

        // The order has to actually be issued first - the button is a shortcut for carrying out
        // an existing Dismissal order, not a way to invent one out of nowhere.
        if (!_records.TryGetRecord<PersonnelRecord>(key, out var record) || record.Status != EmploymentStatus.Dismissal)
        {
            _popup.PopupEntity(Loc.GetString("personnel-dismissal-not-issued"), ent, msg.Actor);
            return;
        }

        if (!_prototype.TryIndex<JobPrototype>(DismissedJobId, out var dismissedJob))
            return;

        _idCard.TryChangeJobTitle(targetId, dismissedJob.LocalizedName, player: msg.Actor);

        if (_prototype.Resolve(dismissedJob.Icon, out var jobIcon))
            _idCard.TryChangeJobIcon(targetId, jobIcon, player: msg.Actor);

        _idCard.TryChangeJobDepartment(targetId, new List<ProtoId<DepartmentPrototype>>());

        if (TryComp<IdCardComponent>(targetId, out var idCardComp))
            idCardComp.JobPrototype = DismissedJobId;

        // Full strip, no exceptions for maintenance/whatever - Passenger itself has no access
        // block at all, so this puts the dismissed person at exactly that floor (§5 point 3).
        _access.TrySetTags(targetId, new List<ProtoId<AccessLevelPrototype>>());

        UpdateGeneralRecord(targetId, dismissedJob.LocalizedName, dismissedJob);

        _adminLogger.Add(LogType.Identity, LogImpact.High,
            $"{ToPrettyString(msg.Actor):actor} dismissed {ToPrettyString(targetId):entity} via {ToPrettyString(ent):console}");

        // PersonnelOrderCompletionSystem picks up the resulting RecordModifiedEvent from
        // UpdateGeneralRecord's Synchronize call and closes out the order from there - this
        // handler's job ends at "the job changed", same as any other way of changing it.
    }

    /// <summary>
    /// Mirrors <c>IdCardConsoleSystem.PrivilegedIdIsAuthorized</c> exactly (that method is
    /// private, and short enough that reimplementing it here is less invasive than widening its
    /// visibility upstream).
    /// </summary>
    private bool PrivilegedIdIsAuthorized(EntityUid uid, IdCardConsoleComponent component)
    {
        if (component.PrivilegedIdSlot.Item is not { Valid: true } id)
            return false;

        if (!TryComp<AccessReaderComponent>(uid, out var reader))
            return true;

        return _accessReader.IsAllowed(id, uid, reader);
    }

    /// <summary>
    /// Mirrors <c>IdCardConsoleSystem.UpdateStationRecord</c> exactly, for the same reason.
    /// </summary>
    private void UpdateGeneralRecord(EntityUid targetId, string jobTitle, JobPrototype? jobProto)
    {
        if (!TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
            || keyStorage.Key is not { } key
            || !_records.TryGetRecord<GeneralStationRecord>(key, out var record))
        {
            return;
        }

        record.JobTitle = jobTitle;

        if (jobProto != null)
        {
            record.JobPrototype = jobProto.ID;
            record.JobIcon = jobProto.Icon;
        }

        _records.Synchronize(key);
    }
}
