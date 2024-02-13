using Content.Shared.CriminalRecords.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Security;
using Content.Shared.Security.Components;

namespace Content.Shared.CriminalRecords.Systems;

public abstract class SharedCriminalRecordsConsoleSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    /// <summary>
    /// Edits the criminal status of a specific identity if the entities true name is equal
    /// to the name of the edited criminal record.
    /// </summary>
    public void UpdateCriminalIdentity(string name, SecurityStatus status)
    {
        var query = EntityQueryEnumerator<IdentityComponent>();

        while (query.MoveNext(out var uid, out var identity))
        {
            if (!Identity.Name(uid, _entityManager).Equals(name))
                continue;

            if (status == SecurityStatus.None)
                RemComp<CriminalRecordComponent>(uid);
            else
                SetCriminalIcon(name, status, uid);
        }
    }

    /// <summary>
    /// Updates the name list stored on every criminal records computer.
    /// </summary>
    public void UpdateCriminalNames(string name, SecurityStatus status)
    {
        var query = EntityQueryEnumerator<CriminalRecordsConsoleComponent>();

        while (query.MoveNext(out var uid, out var recordsConsole))
        {
            if (status == SecurityStatus.None)
                recordsConsole.Criminals.Remove(name);
            else if (!recordsConsole.Criminals.TryAdd(name, status))
                recordsConsole.Criminals[name] = status;
            Dirty(uid, recordsConsole);
        }
    }


    /// <summary>
    /// Decides the icon that should be displayed on the entity based on the security status
    /// </summary>
    public void SetCriminalIcon(string name, SecurityStatus status, EntityUid characterUid)
    {
        EnsureComp<CriminalRecordComponent>(characterUid, out var record);
        record.StatusIcon = status switch
        {
            SecurityStatus.Paroled => "SecurityIconParoled",
            SecurityStatus.Wanted => "SecurityIconWanted",
            SecurityStatus.Detained => "SecurityIconIncarcerated",
            SecurityStatus.Discharged => "SecurityIconDischarged",
            SecurityStatus.Suspected => "SecurityIconSuspected",
            _ => record.StatusIcon
        };
        Dirty(characterUid, record);
    }
}
