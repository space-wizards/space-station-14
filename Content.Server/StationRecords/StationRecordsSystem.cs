using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Shared.Preferences;

public sealed class StationRecordsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialize);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
    }

    private void OnStationInitialize(StationInitializedEvent args)
    {
        AddComp<StationRecordsComponent>(args.Station);
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        CreateGeneralRecord(args.Station, args.Profile, args.JobId);
    }

    private void CreateGeneralRecord(EntityUid station, HumanoidCharacterProfile profile, string? jobId,
        StationRecordsComponent? records = null)
    {
        if (!Resolve(station, ref records))
        {
            return;
        }
    }
}
