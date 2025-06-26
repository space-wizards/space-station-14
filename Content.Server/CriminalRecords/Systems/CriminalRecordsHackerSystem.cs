using Content.Server.Chat.Systems;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.CriminalRecords.Components;
using Content.Shared.CriminalRecords.Systems;
using Content.Shared.Random.Helpers;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.CriminalRecords.Systems;

public sealed class CriminalRecordsHackerSystem : SharedCriminalRecordsHackerSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly CriminalRecordsSystem _criminalRecords = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CriminalRecordsHackerComponent, CriminalRecordsHackDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(Entity<CriminalRecordsHackerComponent> ent, ref CriminalRecordsHackDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        if (_station.GetOwningStation(ent) is not {} station)
            return;

        var reasons = _proto.Index(ent.Comp.Reasons);
        foreach (var (key, record) in _records.GetRecordsOfType<CriminalRecord>(station))
        {
            var reason = _random.Pick(reasons);
            _criminalRecords.OverwriteStatus(new StationRecordKey(key, station), record, SecurityStatus.Wanted, reason);
            // no radio message since spam
            // no history since lazy and its easy to remove anyway
            // main damage with this is existing arrest warrants are lost and to anger beepsky
        }

        _chat.DispatchGlobalAnnouncement(Loc.GetString(ent.Comp.Announcement), playSound: true, colorOverride: Color.Red);

        // once is enough
        RemComp<CriminalRecordsHackerComponent>(ent);

        var ev = new CriminalRecordsHackedEvent(ent, args.Target.Value);
        RaiseLocalEvent(args.User, ref ev);
    }
}

/// <summary>
/// Raised on the user after hacking a criminal records console.
/// </summary>
[ByRefEvent]
public record struct CriminalRecordsHackedEvent(EntityUid User, EntityUid Target);
