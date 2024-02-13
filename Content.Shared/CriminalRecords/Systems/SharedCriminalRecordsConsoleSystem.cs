using Content.Shared.IdentityManagement.Components;
using Content.Shared.Security;

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
            if (!identity.TrueName.Equals(name))
                continue;

            identity.StatusIcon = status switch
            {
                SecurityStatus.None => null,
                SecurityStatus.Paroled => "SecurityIconParoled",
                SecurityStatus.Wanted => "SecurityIconWanted",
                SecurityStatus.Detained => "SecurityIconIncarcerated",
                SecurityStatus.Discharged => "SecurityIconDischarged",
                SecurityStatus.Suspected => "SecurityIconSuspected",
                _ => identity.StatusIcon
            };
            Dirty(uid, identity);
        }
    }
}
