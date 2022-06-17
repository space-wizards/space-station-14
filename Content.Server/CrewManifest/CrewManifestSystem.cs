using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Shared.CrewManifest;
using Robust.Server.GameObjects;

namespace Content.Server.CrewManifest;

public sealed class CrewManifestSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationRecordsSystem _recordsSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    /// <summary>
    ///     Cached crew manifest entries. The alternative is to outright
    ///     rebuild the crew manifest every time the state is requested:
    ///     this is inefficient.
    /// </summary>
    private readonly Dictionary<EntityUid, CrewManifestEntries> _cachedEntries = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<AfterGeneralRecordCreatedEvent>(AfterGeneralRecordCreated);
        SubscribeLocalEvent<RecordModifiedEvent>(OnRecordModified);
        SubscribeLocalEvent<ActiveCrewManifestViewerComponent, BoundUIClosedEvent>(OnBoundUiClose);
    }

    // Not a big fan of this one. Rebuilds the crew manifest every time
    // somebody spawns in, meaning that at round start, it rebuilds the crew manifest
    // wrt the amount of players readied up.
    private void AfterGeneralRecordCreated(AfterGeneralRecordCreatedEvent ev)
    {
        BuildCrewManifest(ev.Key.OriginStation);
    }

    private void OnRecordModified(RecordModifiedEvent ev)
    {
        BuildCrewManifest(ev.Key.OriginStation);
    }

    private void OnBoundUiClose(EntityUid uid, ActiveCrewManifestViewerComponent component, BoundUIClosedEvent ev)
    {
        EntityManager.RemoveComponent<ActiveCrewManifestViewerComponent>(uid);
    }

    /// <summary>
    ///     Builds the crew manifest for a station. Stores it in the cache afterwards.
    /// </summary>
    /// <param name="station"></param>
    private void BuildCrewManifest(EntityUid station)
    {
        var iter = _recordsSystem.GetRecordsOfType<GeneralStationRecord>(station);

        if (iter == null)
        {
            return;
        }

        var entries = new CrewManifestEntries();

        foreach (var recordObject in iter)
        {
            if (recordObject == null)
            {
                continue;
            }

            var record = recordObject.Value.Item2;
            var entry = new CrewManifestEntry(record.Name, record.JobTitle, record.DisplayPriority);

            foreach (var department in record.Departments)
            {
                if (!entries.Entries.TryGetValue(department, out var entryList))
                {
                    entryList = new();
                    entries.Entries.Add(department, entryList);
                }

                entryList.Add(entry);
            }
        }

        if (_cachedEntries.ContainsKey(station))
        {
            _cachedEntries[station] = entries;
        }
        else
        {
            _cachedEntries.Add(station, entries);
        }

        UpdateUserInterface();
    }

    private void UpdateUserInterface()
    {
        foreach (var comp in EntityQuery<ActiveCrewManifestViewerComponent>())
        {
            var owningStation = _stationSystem.GetOwningStation(comp.Owner);
            CrewManifestEntries? entries = null;
            if (owningStation != null)
            {
                _cachedEntries.TryGetValue(owningStation.Value, out entries);
            }

            _uiSystem.GetUiOrNull(comp.Owner, CrewManifestUiKey.Key)?.SetState(new CrewManifestBoundUiState(entries));
        }
    }
}


