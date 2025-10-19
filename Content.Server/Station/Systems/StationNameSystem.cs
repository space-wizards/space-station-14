using Content.Server.Station.Components;
using Content.Shared.Station.Components;

namespace Content.Server.Station.Systems;

/// <summary>
/// This handles naming stations.
/// </summary>
public sealed class StationNameSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _station = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<StationNameSetupComponent, ComponentInit>(OnStationNameSetupInit);
    }

    private void OnStationNameSetupInit(EntityUid uid, StationNameSetupComponent component, ComponentInit args)
    {
        if (!HasComp<StationDataComponent>(uid))
            return;

        _station.RenameStation(uid, GenerateStationName(component), false);
    }

    /// <summary>
    /// Generates a station name from the given config.
    /// </summary>
    private static string GenerateStationName(StationNameSetupComponent config)
    {
        return config.NameGenerator is not null
            ? config.NameGenerator.FormatName(config.StationNameTemplate)
            : config.StationNameTemplate;
    }
}
