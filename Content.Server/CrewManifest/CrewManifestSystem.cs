using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Shared.CrewManifest;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Players;

namespace Content.Server.CrewManifest;

public sealed class CrewManifestSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationRecordsSystem _recordsSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;

    /// <summary>
    ///     Cached crew manifest entries. The alternative is to outright
    ///     rebuild the crew manifest every time the state is requested:
    ///     this is inefficient.
    /// </summary>
    private readonly Dictionary<EntityUid, CrewManifestEntries> _cachedEntries = new();

    private readonly Dictionary<EntityUid, Dictionary<IPlayerSession, CrewManifestEui>> _openEuis = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<AfterGeneralRecordCreatedEvent>(AfterGeneralRecordCreated);
        SubscribeLocalEvent<RecordModifiedEvent>(OnRecordModified);
        SubscribeLocalEvent<ActiveCrewManifestViewerComponent, BoundUIClosedEvent>(OnBoundUiClose);
        SubscribeLocalEvent<CrewManifestViewerComponent, CrewManifestOpenUiMessage>(OpenEuiFromBui);
        SubscribeNetworkEvent<RequestCrewManifestMessage>(OnRequestCrewManifest);
    }

    // As a result of this implementation, the crew manifest does not refresh when somebody
    // joins the game, or if their title is modified. Equally, this should never be opened
    // without ensuring that whatever parent window comes from it also closes, as otherwise
    // this will never close on its own.
    private void OnRequestCrewManifest(RequestCrewManifestMessage message, EntitySessionEventArgs args)
    {

        if (args.SenderSession is not IPlayerSession sessionCast)
        {
            return;
        }

        OpenEui(message.Id, sessionCast);
    }

    // Not a big fan of this one. Rebuilds the crew manifest every time
    // somebody spawns in, meaning that at round start, it rebuilds the crew manifest
    // wrt the amount of players readied up.
    private void AfterGeneralRecordCreated(AfterGeneralRecordCreatedEvent ev)
    {
        BuildCrewManifest(ev.Key.OriginStation);
        UpdateEuis(ev.Key.OriginStation);
    }

    private void OnRecordModified(RecordModifiedEvent ev)
    {
        BuildCrewManifest(ev.Key.OriginStation);
        UpdateEuis(ev.Key.OriginStation);
    }

    private void OnBoundUiClose(EntityUid uid, ActiveCrewManifestViewerComponent component, BoundUIClosedEvent ev)
    {
        component.Viewers--;

        if (component.Viewers == 0)
        {
            EntityManager.RemoveComponent<ActiveCrewManifestViewerComponent>(uid);
        }
    }

    public (string name, CrewManifestEntries? entries) GetCrewManifest(EntityUid station)
    {
        var valid = _cachedEntries.TryGetValue(station, out var manifest);
        return (valid ? MetaData(station).EntityName : string.Empty, valid ? manifest : null);
    }

    private void UpdateEuis(EntityUid station)
    {
        if (_openEuis.TryGetValue(station, out var euis))
        {
            foreach (var eui in euis.Values)
            {
                eui.StateDirty();
            }
        }
    }

    private void OpenEuiFromBui(EntityUid uid, CrewManifestViewerComponent component, CrewManifestOpenUiMessage msg)
    {
        var owningStation = _stationSystem.GetOwningStation(uid);
        if (owningStation == null || msg.Session is not IPlayerSession sessionCast)
        {
            return;
        }

        OpenEui(owningStation.Value, sessionCast);
    }

    public void OpenEui(EntityUid station, IPlayerSession session)
    {
        if (!HasComp<StationRecordsComponent>(station))
        {
            return;
        }

        if (!_openEuis.TryGetValue(station, out var euis))
        {
            euis = new();
        }

        if (euis.ContainsKey(session))
        {
            return;
        }

        var eui = new CrewManifestEui(station, this);
        euis.Add(session, eui);

        _euiManager.OpenEui(eui, session);
    }

    public void CloseEui(EntityUid station, IPlayerSession session)
    {
        if (!HasComp<StationRecordsComponent>(station))
        {
            return;
        }

        if (!_openEuis.TryGetValue(station, out var euis))
        {
            return;
        }

        euis.Remove(session, out var eui);
        eui?.Close();

        if (euis.Count == 0)
        {
            _openEuis.Remove(station);
        }
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
            var entry = new CrewManifestEntry(record.Name, record.JobTitle, record.JobIcon, record.DisplayPriority);

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
            CrewManifestEntries? entries = null;
            if (comp.Station != null)
            {
                _cachedEntries.TryGetValue(comp.Station.Value, out entries);
            }

            _uiSystem.GetUiOrNull(comp.Owner, CrewManifestUiKey.Key)?.SetState(new CrewManifestBoundUiState(entries));
        }
    }

    /// <summary>
    ///     Opens an user interface for a crew manifest.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="player"></param>
    /// <param name="actor"></param>
    public void OpenUserInterface(EntityUid uid, EntityUid player, ActorComponent? actor = null)
    {
        if (!Resolve(player, ref actor))
        {
            return;
        }

        var station = _stationSystem.GetOwningStation(uid);

        OpenUserInterface(uid, station, actor.PlayerSession);
    }

    // Since UI is on freeze, I didn't bother adding in a method for
    // dealing with how to get this from the lobby. The method I
    // thought of included just sending messages to the server
    // to avoid creating a new virtual entity, but at the same time,
    // you could just add the ActiveCrewManifestViewerComponent
    // component to the virtual station. It's a little dirty
    // but it technically works for this instance. If, of course,
    // the BUI doesn't immediately close because we're too
    // far away from the station itself...

    /// <summary>
    ///     Opens an user interface for a crew manifest.
    /// </summary>
    /// <param name="uid">
    ///     Entity to bind this UI to. Can be any entity, so that
    ///     BUI works as needed.
    /// </param>
    /// <param name="station">
    ///     Station that this UI should track. This can be null
    ///     and the UI should display a valid state if this is
    ///     null.
    /// </param>
    /// <param name="player">
    ///     Player to open this UI for.
    /// </param>
    public void OpenUserInterface(EntityUid uid, EntityUid? station, IPlayerSession player)
    {
        if (!_uiSystem.TryGetUi(uid, CrewManifestUiKey.Key, out var bui))
        {
            return;
        }

        var comp = EnsureComp<ActiveCrewManifestViewerComponent>(uid);
        comp.Station = station;
        comp.Viewers++;

        bui.Open(player);
    }
}


