// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.CriminalRecords.Systems;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.CriminalRecords;
using Content.Shared.Database;
using Content.Shared.DeadSpace.PersonnelRecords;
using Content.Shared.DeadSpace.PersonnelRecords.Components;
using Content.Shared.DeadSpace.PersonnelRecords.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Radio;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Security;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationRecords;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.PersonnelRecords.Systems;

/// <summary>
/// Handles all UI and permission logic for the Personnel Records console.
///
/// Visibility scope is never trusted from the client and never cached across actions: every
/// handler re-derives it from whichever ID card <see cref="ActivatableUIComponent.CurrentSingleUser"/>
/// currently holds, exactly as required for a single console prototype that behaves differently
/// depending on who's standing at it.
/// </summary>
public sealed class PersonnelRecordsConsoleSystem : SharedPersonnelRecordsConsoleSystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly CriminalRecordsSystem _criminalRecords = default!;
    [Dependency] private readonly PersonnelRecordsSystem _personnelRecords = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedJobSystem _jobSystem = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PersonnelRecordsConsoleComponent, RecordModifiedEvent>(OnRecordBroadcast);
        SubscribeLocalEvent<PersonnelRecordsConsoleComponent, AfterGeneralRecordCreatedEvent>(OnRecordBroadcast);

        Subs.BuiEvents<PersonnelRecordsConsoleComponent>(PersonnelRecordsConsoleKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
            subs.Event<SelectStationRecord>(OnKeySelected);
            subs.Event<SetStationRecordFilter>(OnFiltersChanged);
            subs.Event<PersonnelRecordSetStatusFilter>(OnStatusFilterPressed);
            subs.Event<PersonnelRecordIssueOrder>(OnIssueOrder);
            subs.Event<PersonnelRecordAnnulOrder>(OnAnnulOrder);
            subs.Event<PersonnelRecordDeclareWanted>(OnDeclareWanted);
        });
    }

    #region BUI plumbing

    private void OnRecordBroadcast<T>(Entity<PersonnelRecordsConsoleComponent> ent, ref T args)
    {
        // Every record change anywhere refreshes every open console - same trade-off
        // CriminalRecordsConsoleSystem makes for the same reason (no per-key push channel).
        UpdateUserInterface(ent);
    }

    private void OnUiOpened(Entity<PersonnelRecordsConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUserInterface(ent, args.Actor);
    }

    private void OnKeySelected(Entity<PersonnelRecordsConsoleComponent> ent, ref SelectStationRecord msg)
    {
        // No concern of a sus client here: record retrieval fails harmlessly on an invalid id.
        ent.Comp.ActiveKey = msg.SelectedKey;
        UpdateUserInterface(ent, msg.Actor);
    }

    private void OnFiltersChanged(Entity<PersonnelRecordsConsoleComponent> ent, ref SetStationRecordFilter msg)
    {
        if (ent.Comp.Filter == null || ent.Comp.Filter.Type != msg.Type || ent.Comp.Filter.Value != msg.Value)
        {
            ent.Comp.Filter = new StationRecordsFilter(msg.Type, msg.Value);
            UpdateUserInterface(ent, msg.Actor);
        }
    }

    private void OnStatusFilterPressed(Entity<PersonnelRecordsConsoleComponent> ent, ref PersonnelRecordSetStatusFilter msg)
    {
        ent.Comp.FilterStatus = msg.FilterStatus;
        UpdateUserInterface(ent, msg.Actor);
    }

    #endregion

    #region Actions

    private void OnIssueOrder(Entity<PersonnelRecordsConsoleComponent> ent, ref PersonnelRecordIssueOrder msg)
    {
        if (msg.Status is not (EmploymentStatus.Reprimand or EmploymentStatus.Demotion or EmploymentStatus.Dismissal))
            return;

        if (!CheckSelected(ent, msg.Actor, out var mob, out var key))
            return;

        if (_timing.CurTime < ent.Comp.NextActionTime)
            return;

        if (!_records.TryGetRecord<GeneralStationRecord>(key.Value, out var general))
            return;

        if (!_records.TryGetRecord<PersonnelRecord>(key.Value, out var record))
            return;

        if (!CanAct(mob.Value, ent, key.Value, general))
        {
            _popup.PopupEntity(Loc.GetString("personnel-records-console-permission-denied"), ent, mob.Value);
            return;
        }

        // Re-validate the ladder here too - console permission checks above don't know the
        // record's current status, PersonnelRecordsSystem.TryIssueOrder is the actual authority.
        var reason = msg.Reason.Trim();
        if (reason.Length < 1 || reason.Length > ent.Comp.MaxStringLength)
            return;

        GetOfficer(mob.Value, out var officer);

        if (!_personnelRecords.TryIssueOrder(key.Value, msg.Status, reason, officer, general.JobPrototype))
            return;

        ent.Comp.NextActionTime = _timing.CurTime + ent.Comp.ActionDelay;

        AnnounceOrder(ent, general, msg.Status, reason, officer);

        _adminLogger.Add(LogType.Identity, LogImpact.Medium,
            $"{ToPrettyString(mob.Value):actor} issued a {msg.Status} personnel order against {general.Name} ({ToPrettyString(ent):console}). Reason: {reason}");

        UpdateUserInterface(ent, mob.Value);
    }

    private void OnAnnulOrder(Entity<PersonnelRecordsConsoleComponent> ent, ref PersonnelRecordAnnulOrder msg)
    {
        if (!CheckSelected(ent, msg.Actor, out var mob, out var key))
            return;

        if (_timing.CurTime < ent.Comp.NextActionTime)
            return;

        if (!_records.TryGetRecord<GeneralStationRecord>(key.Value, out var general))
            return;

        if (!CanAct(mob.Value, ent, key.Value, general))
        {
            _popup.PopupEntity(Loc.GetString("personnel-records-console-permission-denied"), ent, mob.Value);
            return;
        }

        var reason = msg.Reason.Trim();
        if (reason.Length < 1 || reason.Length > ent.Comp.MaxStringLength)
            return;

        GetOfficer(mob.Value, out var officer);

        if (!_personnelRecords.TryAnnulOrder(key.Value, reason, officer))
            return;

        ent.Comp.NextActionTime = _timing.CurTime + ent.Comp.ActionDelay;

        AnnounceAnnul(ent, general, reason, officer);

        _adminLogger.Add(LogType.Identity, LogImpact.Medium,
            $"{ToPrettyString(mob.Value):actor} annulled a personnel order against {general.Name} ({ToPrettyString(ent):console}). Reason: {reason}");

        UpdateUserInterface(ent, mob.Value);
    }

    private void OnDeclareWanted(Entity<PersonnelRecordsConsoleComponent> ent, ref PersonnelRecordDeclareWanted msg)
    {
        if (!CheckSelected(ent, msg.Actor, out var mob, out var key))
            return;

        // No ladder/protected-department checks here - declaring someone wanted is a Criminal
        // Records action being triggered from this console, not a personnel-status transition.
        // Captain/HoS only (DeclareWantedAccess) - deliberately narrower than FullAccess, since the
        // HoP has no business declaring anyone wanted.
        if (!CanDeclareWanted(mob.Value, ent.Comp))
        {
            _popup.PopupEntity(Loc.GetString("personnel-records-console-permission-denied"), ent, mob.Value);
            return;
        }

        var reason = msg.Reason.Trim();
        if (reason.Length < 1 || reason.Length > ent.Comp.MaxStringLength)
            return;

        GetOfficer(mob.Value, out var officer);

        if (!_criminalRecords.TryChangeStatus(key.Value, SecurityStatus.Wanted, reason, officer))
            return;

        _adminLogger.Add(LogType.Identity, LogImpact.Medium,
            $"{ToPrettyString(mob.Value):actor} declared {_records.RecordName(key.Value)} wanted from the Personnel Records console ({ToPrettyString(ent):console}). Reason: {reason}");

        UpdateUserInterface(ent, mob.Value);
    }

    #endregion

    #region Radio announcements

    private void AnnounceOrder(Entity<PersonnelRecordsConsoleComponent> ent, GeneralStationRecord general, EmploymentStatus status, string reason, string officer)
    {
        var args = new (string, object)[] { ("name", general.Name), ("reason", reason), ("officer", officer), ("job", general.JobTitle) };

        var deptLocKey = status switch
        {
            EmploymentStatus.Reprimand => "personnel-records-console-announce-reprimand",
            EmploymentStatus.Demotion => "personnel-records-console-announce-demotion",
            EmploymentStatus.Dismissal => "personnel-records-console-announce-dismissal",
            _ => null,
        };

        if (deptLocKey != null)
            SendToDepartment(ent, general, deptLocKey, args);

        var securityLocKey = status switch
        {
            EmploymentStatus.Demotion => "personnel-records-console-announce-security-demotion",
            EmploymentStatus.Dismissal => "personnel-records-console-announce-security-dismissal",
            _ => null,
        };

        if (securityLocKey != null)
            _radio.SendRadioMessage(ent, Loc.GetString(securityLocKey, args), ent.Comp.SecurityChannel, ent);
    }

    private void AnnounceAnnul(Entity<PersonnelRecordsConsoleComponent> ent, GeneralStationRecord general, string reason, string officer)
    {
        var args = new (string, object)[] { ("name", general.Name), ("reason", reason), ("officer", officer), ("job", general.JobTitle) };

        SendToDepartment(ent, general, "personnel-records-console-announce-annul", args);
        _radio.SendRadioMessage(ent, Loc.GetString("personnel-records-console-announce-annul-security", args), ent.Comp.SecurityChannel, ent);
    }

    private void SendToDepartment(Entity<PersonnelRecordsConsoleComponent> ent, GeneralStationRecord general, string locKey, (string, object)[] args)
    {
        // No department, or a department with no configured channel - only Security ever
        // hears about it in that case (handled separately by each caller).
        if (!_jobSystem.TryGetPrimaryDepartment(general.JobPrototype, out var dept))
            return;

        if (!ent.Comp.DepartmentChannels.TryGetValue(dept.ID, out var channel))
            return;

        _radio.SendRadioMessage(ent, Loc.GetString(locKey, args), channel, ent);
    }

    #endregion

    #region Permissions

    private void GetOfficer(EntityUid uid, out string officer)
    {
        var ev = new TryGetIdentityShortInfoEvent(null, uid);
        RaiseLocalEvent(ev);
        officer = ev.Title ?? Loc.GetString("personnel-records-console-unknown-officer");
    }

    /// <summary>
    /// Boilerplate most actions use: verifies the console's own AccessReader, blocks silicons
    /// outright (AI/borgs are read-only on this console, see below), and resolves the active key.
    /// Does not check anything about the *target* - see <see cref="CanAct"/> for that.
    /// </summary>
    private bool CheckSelected(Entity<PersonnelRecordsConsoleComponent> ent, EntityUid user,
        [NotNullWhen(true)] out EntityUid? mob, [NotNullWhen(true)] out StationRecordKey? key)
    {
        key = null;
        mob = null;

        // The console sits on BaseComputerAiAccess so the AI (and, transitively, borgs - they share
        // the same silicon access whitelist) can open and read it, but AccessReaderSystem.IsAllowed
        // trivially passes for anything when the target has no AccessReaderComponent (true here) -
        // so read-only for silicons has to be enforced explicitly rather than relying on that as a
        // side effect. Borgs were slipping through here before: only the AI's own eye entity
        // (StationAiHeldComponent) was checked, not the borg chassis itself.
        if (HasComp<StationAiHeldComponent>(user) || HasComp<BorgChassisComponent>(user))
            return false;

        if (!_access.IsAllowed(user, ent))
        {
            _popup.PopupEntity(Loc.GetString("personnel-records-console-permission-denied"), ent, user);
            return false;
        }

        if (ent.Comp.ActiveKey is not { } id)
            return false;

        if (_station.GetOwningStation(ent) is not { } station)
            return false;

        key = new StationRecordKey(id, station);
        mob = user;
        return true;
    }

    /// <summary>
    /// Full permission check for acting against a specific target: holds the console's own base
    /// access, target is visible to this user, not the user's own record, and - if the target is
    /// a department head - the user has <see cref="PersonnelRecordsConsoleComponent.ProtectedAccess"/>
    /// (the captain).
    /// </summary>
    private bool CanAct(EntityUid user, Entity<PersonnelRecordsConsoleComponent> ent, StationRecordKey targetKey, GeneralStationRecord targetGeneral)
    {
        if (HasComp<StationAiHeldComponent>(user) || HasComp<BorgChassisComponent>(user))
            return false;

        // Base gate: without the console's own access requirement (Command), department
        // membership alone must not grant scope - a rank-and-file crewmember (any primary
        // department, but no Command-family access) should see and act on nothing here.
        if (!_access.IsAllowed(user, ent))
            return false;

        var console = ent.Comp;
        var fullAccess = HasFullAccess(user, console);
        TryGetUserDepartment(user, out var userDept);

        if (!IsVisible(targetGeneral, console, fullAccess, userDept))
            return false;

        if (IsSelf(user, targetKey))
            return false;

        if (IsProtectedTarget(console, targetGeneral, out var requiredAccess) && !HasAccessTag(user, requiredAccess))
            return false;

        return true;
    }

    private bool IsSelf(EntityUid user, StationRecordKey targetKey)
    {
        if (!_idCard.TryFindIdCard(user, out var idCard))
            return false;

        if (!TryComp<StationRecordKeyStorageComponent>(idCard, out var keyStorage) || keyStorage.Key is not { } ownKey)
            return false;

        return ownKey.Equals(targetKey);
    }

    /// <summary>
    /// Whether the target needs some elevated access to be acted against, and if so, which. Jobs
    /// (<see cref="PersonnelRecordsConsoleComponent.ProtectedJobs"/> - Captain, IAA, BlueShieldOfficer)
    /// are checked before departments, so e.g. the captain always needs Central Command access even
    /// though they'd otherwise also match the Command department protection.
    /// </summary>
    private bool IsProtectedTarget(PersonnelRecordsConsoleComponent console, GeneralStationRecord targetGeneral, out ProtoId<AccessLevelPrototype> requiredAccess)
    {
        if (console.ProtectedJobs.Contains(targetGeneral.JobPrototype))
        {
            requiredAccess = console.ProtectedJobsAccess;
            return true;
        }

        foreach (var deptId in console.ProtectedDepartments)
        {
            if (_prototype.TryIndex(deptId, out var dept) && dept.Roles.Contains(targetGeneral.JobPrototype))
            {
                requiredAccess = console.ProtectedAccess;
                return true;
            }
        }

        requiredAccess = default;
        return false;
    }

    private bool HasAccessTag(EntityUid user, ProtoId<AccessLevelPrototype> tag)
    {
        return _access.FindAccessTags(user).Contains(tag);
    }

    private bool HasFullAccess(EntityUid user, PersonnelRecordsConsoleComponent console) =>
        HasAnyAccessTag(user, console.FullAccess);

    private bool CanDeclareWanted(EntityUid user, PersonnelRecordsConsoleComponent console) =>
        HasAnyAccessTag(user, console.DeclareWantedAccess);

    private bool HasAnyAccessTag(EntityUid user, List<ProtoId<AccessLevelPrototype>> allowedTags)
    {
        var tags = _access.FindAccessTags(user);
        foreach (var allowed in allowedTags)
        {
            if (tags.Contains(allowed))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Deliberately does NOT read <c>IdCardComponent.JobPrototype</c> - that field is only ever
    /// written by the ID card console's own edit flow (<c>IdCardConsoleSystem.TryWriteToTargetId</c>)
    /// and by <c>PersonnelDismissalSystem</c>; nothing sets it at round-start spawn
    /// (<c>StationSpawningSystem.SetPdaAndIdCardData</c> sets the job title/icon/access, never this
    /// field), so it stays null for anyone whose card nobody has rewritten since spawn - which is
    /// everyone except Captain/HoP, who never hit this path at all (they go through the
    /// full-access branch instead). That made every other department head's own console
    /// permanently show "department not determined". <see cref="GeneralStationRecord.JobPrototype"/>
    /// via the card's own <see cref="StationRecordKeyStorageComponent"/> is reliable instead - it's
    /// the same source the crew manifest and everything else in this feature already trusts.
    /// </summary>
    private bool TryGetUserDepartment(EntityUid user, out DepartmentPrototype? department)
    {
        department = null;

        if (!_idCard.TryFindIdCard(user, out var idCard))
            return false;

        if (!TryComp<StationRecordKeyStorageComponent>(idCard, out var keyStorage) || keyStorage.Key is not { } key)
            return false;

        if (!_records.TryGetRecord<GeneralStationRecord>(key, out var general))
            return false;

        return _jobSystem.TryGetPrimaryDepartment(general.JobPrototype, out department);
    }

    /// <summary>
    /// Visibility, independent of any specific action: excluded jobs and blacklisted departments
    /// are never shown to anyone; everything else is shown to full-access users, and to
    /// department-scoped users only within their own primary department.
    ///
    /// Deliberately does NOT treat "no primary department" as invisible - <c>Command</c> is
    /// <c>primary: false</c> in departments.yml (every other head also belongs to their own
    /// department, which *is* primary, so this never mattered for them), and the Captain has no
    /// other department at all. Without this, the captain could never be visible here no matter
    /// what access the viewer had, full or otherwise.
    /// </summary>
    private bool IsVisible(GeneralStationRecord general, PersonnelRecordsConsoleComponent console, bool fullAccess, DepartmentPrototype? userDept)
    {
        if (console.ExcludedJobs.Contains(general.JobPrototype))
            return false;

        _jobSystem.TryGetPrimaryDepartment(general.JobPrototype, out var targetDept);

        if (targetDept != null && console.BlacklistedDepartments.Contains(targetDept.ID))
            return false;

        if (fullAccess)
            return true;

        return userDept != null && targetDept != null && targetDept.ID == userDept.ID;
    }

    private bool IsVisibleToUser(uint id, EntityUid station, PersonnelRecordsConsoleComponent console, bool fullAccess, DepartmentPrototype? userDept)
    {
        var key = new StationRecordKey(id, station);
        return _records.TryGetRecord<GeneralStationRecord>(key, out var general)
            && IsVisible(general, console, fullAccess, userDept);
    }

    #endregion

    #region State

    /// <summary>
    /// Refresh triggered by something other than a direct BUI message (a record changed
    /// somewhere on the station) - there's no actor in hand, so fall back to whoever the
    /// console's <c>ActivatableUI</c> currently considers its single user, if anyone.
    /// </summary>
    private void UpdateUserInterface(Entity<PersonnelRecordsConsoleComponent> ent)
    {
        var actor = TryComp<ActivatableUIComponent>(ent, out var activatable) ? activatable.CurrentSingleUser : null;
        UpdateUserInterface(ent, actor ?? EntityUid.Invalid);
    }

    private void UpdateUserInterface(Entity<PersonnelRecordsConsoleComponent> ent, EntityUid actor)
    {
        var (uid, console) = ent;
        var owningStation = _station.GetOwningStation(uid);

        if (!TryComp<StationRecordsComponent>(owningStation, out var stationRecords))
        {
            _ui.SetUiState(uid, PersonnelRecordsConsoleKey.Key, new PersonnelRecordsConsoleState());
            return;
        }

        // Without the console's own base access (Command), there is no scope to speak of at all -
        // same "department not determined" outcome as having no primary department, see CanAct.
        var hasBaseAccess = _access.IsAllowed(actor, ent);
        var fullAccess = false;
        DepartmentPrototype? userDept = null;
        var hasDepartment = false;

        if (hasBaseAccess)
        {
            fullAccess = HasFullAccess(actor, console);
            hasDepartment = TryGetUserDepartment(actor, out userDept);
        }

        var listing = hasBaseAccess
            ? _records.BuildListing((owningStation.Value, stationRecords), console.Filter)
                .Where(x => IsVisibleToUser(x.Key, owningStation.Value, console, fullAccess, userDept))
                .ToDictionary(x => x.Key, x => x.Value)
            : new Dictionary<uint, string>();

        if (console.FilterStatus != EmploymentStatus.None)
        {
            listing = listing
                .Where(x => _records.TryGetRecord<PersonnelRecord>(new StationRecordKey(x.Key, owningStation.Value), out var record)
                    && record.Status == console.FilterStatus)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        var state = new PersonnelRecordsConsoleState(listing, console.Filter)
        {
            FilterStatus = console.FilterStatus,
            FullAccess = fullAccess,
            NoDepartment = !fullAccess && !hasDepartment,
        };

        if (console.ActiveKey is { } id)
        {
            var key = new StationRecordKey(id, owningStation.Value);
            if (hasBaseAccess
                && _records.TryGetRecord<GeneralStationRecord>(key, out var general, stationRecords)
                && IsVisible(general, console, fullAccess, userDept))
            {
                state.StationRecord = general;
                _records.TryGetRecord(key, out state.PersonnelRecord, stationRecords);
                _records.TryGetRecord(key, out state.CriminalRecord, stationRecords);
                state.SelectedKey = id;

                if (state.PersonnelRecord != null)
                {
                    var canAct = CanAct(actor, ent, key, general);
                    var status = state.PersonnelRecord.Status;

                    state.CanReprimand = canAct && status == EmploymentStatus.None;
                    state.CanDemote = canAct && status is EmploymentStatus.None or EmploymentStatus.Reprimand;
                    state.CanDismiss = state.CanDemote;
                    state.CanAnnul = canAct && status is EmploymentStatus.Demotion or EmploymentStatus.Dismissal;
                    state.CanPrint = status != EmploymentStatus.None;
                    state.CanDeclareWanted = CanDeclareWanted(actor, console);
                }
            }
            else
            {
                // ActiveKey is shared by the console entity, not by an individual BUI session.
                // Never retain a selection that the current user cannot see: otherwise the next
                // state update could disclose another department's records.
                console.ActiveKey = null;
            }
        }

        _ui.SetUiState(uid, PersonnelRecordsConsoleKey.Key, state);
    }

    #endregion

    #region External accessors

    // PersonnelRecordsConsoleComponent carries [Access(typeof(SharedPersonnelRecordsConsoleSystem))],
    // so PersonnelOrderCompletionSystem and PersonnelVacancySystem (Phase 5) can't read its fields
    // directly despite needing the same station-wide config (department channels, exclusions,
    // FreeSlotOnDemotion) - they go through this system's public API instead, same as any other
    // cross-system access to an [Access]-restricted component.

    public bool IsJobExcluded(PersonnelRecordsConsoleComponent console, ProtoId<JobPrototype> job) =>
        console.ExcludedJobs.Contains(job);

    public bool IsDepartmentBlacklisted(PersonnelRecordsConsoleComponent console, ProtoId<DepartmentPrototype> department) =>
        console.BlacklistedDepartments.Contains(department);

    public bool TryGetDepartmentChannel(PersonnelRecordsConsoleComponent console, ProtoId<DepartmentPrototype> department, out ProtoId<RadioChannelPrototype> channel) =>
        console.DepartmentChannels.TryGetValue(department, out channel);

    public ProtoId<RadioChannelPrototype> GetSecurityChannel(PersonnelRecordsConsoleComponent console) =>
        console.SecurityChannel;

    public bool GetFreeSlotOnDemotion(PersonnelRecordsConsoleComponent console) =>
        console.FreeSlotOnDemotion;

    /// <summary>
    /// Public wrapper for <see cref="CheckSelected"/> - <c>PersonnelPrintingSystem</c> needs the
    /// exact same boilerplate (AI block, base access, active-key/station resolution) every other
    /// action already goes through, rather than a second copy of it.
    /// </summary>
    public bool TryCheckSelected(Entity<PersonnelRecordsConsoleComponent> ent, EntityUid user,
        [NotNullWhen(true)] out EntityUid? mob, [NotNullWhen(true)] out StationRecordKey? key) =>
        CheckSelected(ent, user, out mob, out key);

    /// <summary>
    /// Visibility only, deliberately without the self/protected-department restrictions in
    /// <see cref="CanAct"/> - printing doesn't change any game state (the order already exists;
    /// the paper is just its physical record), so "anyone who can see the record" is the whole
    /// rule (§2.2 "Кто может печатать").
    /// </summary>
    public bool CanView(EntityUid user, PersonnelRecordsConsoleComponent console, GeneralStationRecord targetGeneral)
    {
        var fullAccess = HasFullAccess(user, console);
        TryGetUserDepartment(user, out var userDept);
        return IsVisible(targetGeneral, console, fullAccess, userDept);
    }

    /// <summary>
    /// Lets <c>PersonnelPrintingSystem</c> set its own print cooldown without write access to the
    /// component - same reasoning as the read-only accessors above.
    /// </summary>
    public void SetNextPrintTime(PersonnelRecordsConsoleComponent console, TimeSpan time)
    {
        console.NextPrintTime = time;
    }

    #endregion
}
