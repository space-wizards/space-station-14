using Content.Server.StationEvents.Components;
using Content.Server.Announcements.Systems;
using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Lock;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Player;

namespace Content.Server.StationEvents.Events;


/// <summary>
///     Greytide Virus event
///     This will open and bolt airlocks and unlock lockers from randomly selected access groups.
/// </summary>
public sealed class GreytideVirusRule : StationEventSystem<GreytideVirusRuleComponent>
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AnnouncerSystem _announcer = default!;

    protected override void Added(EntityUid uid, GreytideVirusRuleComponent virusComp, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        // pick severity randomly from range if not specified otherwise
        virusComp.Severity ??= virusComp.SeverityRange.Next(_random);
        virusComp.Severity = Math.Min(virusComp.Severity.Value, virusComp.AccessGroups.Count);
        _announcer.SendAnnouncement(
            _announcer.GetAnnouncementId(args.RuleId),
            Filter.Broadcast(),
            Loc.GetString("station-event-greytide-virus-start-announcement", ("severity", virusComp.Severity.Value)),
            null,
            Color.Gold
        );
        base.Added(uid, virusComp, gameRule, args);
    }
    protected override void Started(EntityUid uid, GreytideVirusRuleComponent virusComp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, virusComp, gameRule, args);

        if (virusComp.Severity == null)
            return;

        // pick random access groups
        var chosen = _random.GetItems(virusComp.AccessGroups, virusComp.Severity.Value, allowDuplicates: false);

        // combine all the selected access groups
        var accessIds = new HashSet<ProtoId<AccessLevelPrototype>>();
        foreach (var group in chosen)
        {
            if (_prototype.TryIndex(group, out var proto))
                accessIds.UnionWith(proto.Tags);
        }

        var firelockQuery = GetEntityQuery<FirelockComponent>();
        var accessQuery = GetEntityQuery<AccessReaderComponent>();

        var lockQuery = AllEntityQuery<LockComponent>();
        while (lockQuery.MoveNext(out var lockUid, out var lockComp))
        {
            if (!accessQuery.TryComp(lockUid, out var accessComp))
                continue;

            // check access
            // the AreAccessTagsAllowed function is a little weird because it technically has support for certain tags to be locked out of opening something
            // which might have unintened side effects (see the comments in the function itself)
            // but no one uses that yet, so it is fine for now
            if (!_access.AreAccessTagsAllowed(accessIds, accessComp) || _access.AreAccessTagsAllowed(virusComp.Blacklist, accessComp))
                continue;

            // open lockers
            _lock.Unlock(lockUid, null, lockComp);
        }

        var airlockQuery = AllEntityQuery<AirlockComponent, DoorComponent>();
        while (airlockQuery.MoveNext(out var airlockUid, out var airlockComp, out var doorComp))
        {
            // don't space everything
            if (firelockQuery.HasComp(airlockUid))
                continue;

            // use the access reader from the door electronics if they exist
            if (!_access.GetMainAccessReader(airlockUid, out var accessComp))
                continue;

            // check access
            if (!_access.AreAccessTagsAllowed(accessIds, accessComp) || _access.AreAccessTagsAllowed(virusComp.Blacklist, accessComp))
                continue;

            // open and bolt airlocks
            _door.TryOpenAndBolt(airlockUid, doorComp, airlockComp);
        }
    }
}
