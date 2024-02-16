using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Security;
using Content.Shared.Security.Components;

namespace Content.Shared.CriminalRecords.Systems;

public abstract class SharedCriminalRecordsConsoleSystem : EntitySystem
{
    /// <summary>
    /// Edits the criminal status of a specific identity if the entities true name is equal
    /// to the name of the edited criminal record.
    /// </summary>
    public void UpdateCriminalIdentity(string name, SecurityStatus status)
    {
        var query = EntityQueryEnumerator<IdentityComponent>();

        while (query.MoveNext(out var uid, out var identity))
        {
            if (!Identity.Name(uid, EntityManager).Equals(name))
                continue;

            if (status == SecurityStatus.None)
                RemComp<CriminalRecordComponent>(uid);
            else
                SetCriminalIcon(name, status, uid);
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
