using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Systems;

namespace Content.Client.StationRecords;

public sealed partial class GeneralStationRecordConsoleSystem : SharedGeneralStationRecordConsoleSystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    protected override void UpdateUserInterface(Entity<GeneralStationRecordConsoleComponent> ent)
    {
        if (_ui.TryGetOpenUi(ent.Owner, GeneralStationRecordConsoleKey.Key, out var bui))
            bui.Update();
    }

    // Needed so when a record is created or deleted, it appears in the UI instantly.
    [SubscribeLocalEvent]
    private void OnAfterHandleState(Entity<StationRecordsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var query = EntityQueryEnumerator<GeneralStationRecordConsoleComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (_ui.TryGetOpenUi(uid, GeneralStationRecordConsoleKey.Key, out var bui))
                bui.Update();
        }
    }
}
