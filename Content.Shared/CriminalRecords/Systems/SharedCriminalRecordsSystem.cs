using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Security;
using Content.Shared.Security.Components;
using Content.Shared.StationRecords;
using Robust.Shared.Serialization;

namespace Content.Shared.CriminalRecords.Systems;

public abstract class SharedCriminalRecordsSystem : EntitySystem
{
    /// <summary>
    /// Any entity that has a the name of the record that was just changed as their visible name will get their icon
    /// updated with the new status, if the record got removed their icon will be removed too.
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

        var previousIcon = record.StatusIcon;

        record.StatusIcon = status switch
        {
            SecurityStatus.Paroled => "SecurityIconParoled",
            SecurityStatus.Wanted => "SecurityIconWanted",
            SecurityStatus.Detained => "SecurityIconIncarcerated",
            SecurityStatus.Discharged => "SecurityIconDischarged",
            SecurityStatus.Suspected => "SecurityIconSuspected",
            SecurityStatus.Hostile => "SecurityIconHostile",
            SecurityStatus.Eliminated => "SecurityIconEliminated",
            _ => record.StatusIcon
        };

        if (previousIcon != record.StatusIcon)
            Dirty(characterUid, record);
    }
}

[Serializable, NetSerializable]
public struct WantedRecord(GeneralStationRecord targetInfo, SecurityStatus status, string? reason, string? initiator, List<CrimeHistory> history)
{
    public GeneralStationRecord TargetInfo = targetInfo;
    public SecurityStatus Status = status;
    public string? Reason = reason;
    public string? Initiator = initiator;
    public List<CrimeHistory> History = history;
};

[ByRefEvent]
public record struct CriminalRecordChangedEvent(CriminalRecord Record);

[ByRefEvent]
public record struct CriminalHistoryAddedEvent(CrimeHistory History);

[ByRefEvent]
public record struct CriminalHistoryRemovedEvent(CrimeHistory History);
