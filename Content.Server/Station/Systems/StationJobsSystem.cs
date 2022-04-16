using Content.Server.Station.Components;

namespace Content.Server.Station.Systems;

public sealed class StationJobsSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialized);
    }

    private void OnStationInitialized(StationInitializedEvent msg, EntitySessionEventArgs args)
    {
        var stationJobs = AddComp<StationJobsComponent>(msg.Station);

    }
}
