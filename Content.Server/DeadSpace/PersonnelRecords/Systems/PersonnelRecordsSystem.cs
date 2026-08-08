// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.GameTicking;
using Content.Server.StationRecords.Systems;
using Content.Shared.DeadSpace.PersonnelRecords;
using Content.Shared.DeadSpace.PersonnelRecords.Components;
using Content.Shared.DeadSpace.PersonnelRecords.Systems;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.PersonnelRecords.Systems;

/// <summary>
/// Personnel records - the HR counterpart to Criminal Records.
///
/// Owns creation of the <see cref="PersonnelRecord"/> that rides alongside every crewmember's
/// <c>GeneralStationRecord</c>, and the status-transition logic (issue/annul an order) that
/// <c>PersonnelRecordsConsoleSystem</c> calls into after its own permission checks pass.
/// Order-execution detection (<c>PersonnelOrderCompletionSystem</c>) is added in Phase 5.
/// </summary>
public sealed class PersonnelRecordsSystem : SharedPersonnelRecordsSystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AfterGeneralRecordCreatedEvent>(OnGeneralRecordCreated);
    }

    private void OnGeneralRecordCreated(AfterGeneralRecordCreatedEvent ev)
    {
        _records.AddRecordEntry(ev.Key, new PersonnelRecord());
        _records.Synchronize(ev.Key);
    }

    /// <summary>
    /// Issues a Reprimand, Demotion or Dismissal order. Enforces the "ladder": a reprimand only
    /// from None, a demotion/dismissal from either None or Reprimand. Does not perform any
    /// permission/visibility checks - the caller (<c>PersonnelRecordsConsoleSystem</c>) is
    /// responsible for that.
    /// </summary>
    /// <param name="key">The record to act on.</param>
    /// <param name="status">The order being issued - must be Reprimand, Demotion or Dismissal.</param>
    /// <param name="reason">Mandatory reason, already trimmed/length-checked by the caller.</param>
    /// <param name="initiatorName">Name-by-ID of whoever issued the order.</param>
    /// <param name="currentJob">The target's current job prototype, snapshotted into
    /// <see cref="PersonnelRecord.JobAtOrder"/> so execution can later be detected.</param>
    /// <returns>True if the order was issued, false if the record doesn't exist or the
    /// transition isn't legal from the record's current status.</returns>
    public bool TryIssueOrder(StationRecordKey key, EmploymentStatus status, string reason, string? initiatorName, ProtoId<JobPrototype>? currentJob)
    {
        if (!_records.TryGetRecord<PersonnelRecord>(key, out var record))
            return false;

        PersonnelActionType actionType;
        switch (status)
        {
            case EmploymentStatus.Reprimand:
                if (record.Status != EmploymentStatus.None)
                    return false;
                actionType = PersonnelActionType.Reprimand;
                break;
            case EmploymentStatus.Demotion:
                if (record.Status != EmploymentStatus.None && record.Status != EmploymentStatus.Reprimand)
                    return false;
                actionType = PersonnelActionType.Demotion;
                break;
            case EmploymentStatus.Dismissal:
                if (record.Status != EmploymentStatus.None && record.Status != EmploymentStatus.Reprimand)
                    return false;
                actionType = PersonnelActionType.Dismissal;
                break;
            default:
                // None isn't a valid thing to "issue".
                return false;
        }

        // Only Demotion/Dismissal need to remember where to fall back to on annul, and what job
        // to watch for a change away from to detect execution. A fresh Reprimand from None has
        // nothing to remember - StatusBeforeOrder/JobAtOrder stay null.
        if (status is EmploymentStatus.Demotion or EmploymentStatus.Dismissal)
        {
            if (!_records.TryGetRecord<GeneralStationRecord>(key, out var general))
                return false;

            record.StatusBeforeOrder = record.Status;
            record.JobAtOrder = currentJob;
            record.JobTitleAtOrder = general.JobTitle;
        }

        record.Status = status;
        record.Reason = reason;
        record.InitiatorName = initiatorName;

        AddHistory(record, actionType, reason, initiatorName);
        _records.Synchronize(key);

        SyncIdentityIcon(key, status);

        var changedEvent = new PersonnelStatusChangedEvent(key, actionType, status, reason, initiatorName);
        RaiseLocalEvent(ref changedEvent);

        return true;
    }

    /// <summary>
    /// Cancels an active Demotion/Dismissal order, restoring <see cref="PersonnelRecord.StatusBeforeOrder"/>
    /// (None, or Reprimand if one was already active before the order was issued). Only legal
    /// while the order hasn't been executed yet - once <c>PersonnelOrderCompletionSystem</c> (Phase 5)
    /// resets an executed record to None, there's nothing left to annul.
    /// </summary>
    public bool TryAnnulOrder(StationRecordKey key, string reason, string? initiatorName)
    {
        if (!_records.TryGetRecord<PersonnelRecord>(key, out var record))
            return false;

        if (record.Status != EmploymentStatus.Demotion && record.Status != EmploymentStatus.Dismissal)
            return false;

        var target = record.StatusBeforeOrder ?? EmploymentStatus.None;

        record.Status = target;
        record.JobAtOrder = null;
        record.JobTitleAtOrder = null;
        record.StatusBeforeOrder = null;

        if (target == EmploymentStatus.Reprimand)
        {
            // Restore the still-active reprimand's own reason/initiator instead of leaving the
            // annulled order's reason in place. Guaranteed to exist: StatusBeforeOrder is only
            // ever set to Reprimand from a real Reprimand issuance, which always logs one.
            var lastReprimand = record.History.FindLast(h => h.Type == PersonnelActionType.Reprimand);
            record.Reason = lastReprimand.Text;
            record.InitiatorName = lastReprimand.InitiatorName;
        }
        else
        {
            record.Reason = null;
            record.InitiatorName = null;
        }

        AddHistory(record, PersonnelActionType.Annul, reason, initiatorName);
        _records.Synchronize(key);

        SyncIdentityIcon(key, target);

        var changedEvent = new PersonnelStatusChangedEvent(key, PersonnelActionType.Annul, target, reason, initiatorName);
        RaiseLocalEvent(ref changedEvent);

        return true;
    }

    /// <summary>
    /// Closes out an executed Demotion/Dismissal order. Called by
    /// <c>PersonnelOrderCompletionSystem</c> once it detects that the target's job no longer
    /// matches <see cref="PersonnelRecord.JobAtOrder"/> - however that change actually happened
    /// (the ID card console's dismiss button, a manual job change, an admin, CentCom). Status
    /// always lands at None regardless of what preceded the order; unlike annul, execution never
    /// restores a prior Reprimand - the person has moved on to a new job with a clean slate.
    /// </summary>
    /// <param name="key">The record to close out.</param>
    /// <returns>True if an order was actually closed out, false if the record had nothing pending.</returns>
    public bool TryExecuteOrder(StationRecordKey key)
    {
        if (!_records.TryGetRecord<PersonnelRecord>(key, out var record))
            return false;

        if (record.Status != EmploymentStatus.Demotion && record.Status != EmploymentStatus.Dismissal)
            return false;

        if (record.JobAtOrder is not { } executedJob)
            return false;

        if (record.JobTitleAtOrder is not { } previousJobTitle)
            return false;

        var previousStatus = record.Status;

        record.PreviousJobTitle = previousJobTitle;
        record.Status = EmploymentStatus.None;
        record.Reason = null;
        record.InitiatorName = null;
        record.JobAtOrder = null;
        record.JobTitleAtOrder = null;
        record.StatusBeforeOrder = null;

        AddHistory(record, PersonnelActionType.Executed, Loc.GetString("personnel-records-history-auto-executed", ("job", previousJobTitle)), null);
        _records.Synchronize(key);

        SyncIdentityIcon(key, EmploymentStatus.None);

        var changedEvent = new PersonnelStatusChangedEvent(key, PersonnelActionType.Executed, EmploymentStatus.None, null, null);
        RaiseLocalEvent(ref changedEvent);

        var executedEvent = new PersonnelOrderExecutedEvent(key, executedJob, previousStatus);
        RaiseLocalEvent(ref executedEvent);

        return true;
    }

    private void AddHistory(PersonnelRecord record, PersonnelActionType type, string text, string? initiatorName)
    {
        record.History.Add(new PersonnelHistory(_ticker.RoundDuration(), type, text, initiatorName));
    }

    /// <summary>
    /// Refreshes the HUD icon on whoever holds this record's ID card right now.
    ///
    /// Resolves the live character directly through <see cref="StationRecordKeyStorageComponent"/>
    /// (walking up the container chain from the card to whoever is wearing/holding it) rather than
    /// matching by <c>Identity.Name</c> - the "look up by current visible name" trick
    /// <c>CriminalRecordsSystem</c> uses, which silently finds nobody whenever the target's identity
    /// is obscured (mask, hood, anything with an <c>IdentityBlockerComponent</c>): with no viewer,
    /// <see cref="Identity.Name"/> always resolves to the disguised name, never the real one, so the
    /// name search comes up empty and the icon never gets attached even though the order itself went
    /// through fine. The name-based lookup is kept purely as a fallback for the rare case the card
    /// can't be resolved at all (lost, destroyed, or otherwise not currently held by anyone).
    /// </summary>
    private void SyncIdentityIcon(StationRecordKey key, EmploymentStatus status)
    {
        if (TryGetRecordCharacter(key, out var character))
        {
            if (status is EmploymentStatus.Demotion or EmploymentStatus.Dismissal)
                SetPersonnelIcon(status, character);
            else
                RemComp<PersonnelRecordComponent>(character);

            return;
        }

        var name = _records.RecordName(key);
        if (name != string.Empty)
            UpdatePersonnelIdentity(name, status);
    }

    /// <summary>
    /// Finds the ID card/PDA holding this record's key, then walks up the container chain (id
    /// directly worn, or id tucked inside a PDA that's worn) until it reaches an entity with an
    /// <see cref="IdentityComponent"/> - i.e. an actual character, not just another piece of
    /// equipment on the way there.
    /// </summary>
    private bool TryGetRecordCharacter(StationRecordKey key, out EntityUid character)
    {
        character = default;

        EntityUid? holder = null;
        var query = EntityQueryEnumerator<StationRecordKeyStorageComponent>();
        while (query.MoveNext(out var uid, out var keyStorage))
        {
            if (keyStorage.Key is { } candidateKey && candidateKey.Equals(key))
            {
                holder = uid;
                break;
            }
        }

        if (holder is not { } current)
            return false;

        // A handful of hops is plenty for "id in a PDA in a pocket/slot" - bail out rather than loop
        // forever if something unexpected is holding the card.
        for (var i = 0; i < 4; i++)
        {
            if (HasComp<IdentityComponent>(current))
            {
                character = current;
                return true;
            }

            if (!_container.TryGetContainingContainer(current, out var container))
                return false;

            current = container.Owner;
        }

        return false;
    }
}
