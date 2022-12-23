using System.Linq;
using System.Text.Json.Nodes;
using Content.Server.Administration;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.CrewManifest;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Robust.Server.Player;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.CrewManifest;

public sealed class CrewManifestSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationRecordsSystem _recordsSystem = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IStatusHost _status = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

    /// <summary>
    ///     Used for thread safety, given <see cref="IStatusHost.OnStatusRequest"/> is called from another thread.
    /// </summary>
    private readonly object _statusShellLock = new();

    /// <summary>
    ///     Cached crew manifest entries. The alternative is to outright
    ///     rebuild the crew manifest every time the state is requested:
    ///     this is inefficient.
    /// </summary>
    private readonly Dictionary<EntityUid, CrewManifestEntries> _cachedEntries = new();

    // An utter hack because departments store jobs and not the other way around.
    private readonly Dictionary<string, HashSet<string>> _jobDepartments = new();

    private bool _crewManifestWithoutEntity;

    private readonly Dictionary<EntityUid, Dictionary<IPlayerSession, CrewManifestEui>> _openEuis = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<AfterGeneralRecordCreatedEvent>(AfterGeneralRecordCreated);
        SubscribeLocalEvent<RecordModifiedEvent>(OnRecordModified);
        SubscribeLocalEvent<StationRecordsComponent, StationRenamedEvent>(OnStationRenamed);
        SubscribeLocalEvent<CrewManifestViewerComponent, BoundUIClosedEvent>(OnBoundUiClose);
        SubscribeLocalEvent<CrewManifestViewerComponent, CrewManifestOpenUiMessage>(OpenEuiFromBui);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeNetworkEvent<RequestCrewManifestMessage>(OnRequestCrewManifest);

        _gameTicker.OnManifestRequest += GetStatusResponse;
        _prototype.PrototypesReloaded += OnPrototypesReloaded;
        _configManager.OnValueChanged(CCVars.CrewManifestWithoutEntity, b => _crewManifestWithoutEntity = b, true);
        BuildDeptIndex();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        BuildDeptIndex();
    }

    private void BuildDeptIndex()
    {
        foreach (var dept in _prototype.EnumeratePrototypes<DepartmentPrototype>())
        {
            foreach (var job in dept.Roles)
            {
                if (!_jobDepartments.ContainsKey(job))
                {
                    _jobDepartments[job] = new() { dept.ID };
                    continue;
                }

                _jobDepartments[job].Add(dept.ID);
            }
        }
    }

    private void OnStationRenamed(EntityUid uid, StationRecordsComponent component, StationRenamedEvent args)
    {
        BuildCrewManifest(uid);
    }

    private void GetStatusResponse(JsonNode jObject)
    {
        if (!_crewManifestWithoutEntity)
            return;

        // THREAD SAFETY: We're on a network thread, not the game thread, make sure nothing edits the cache underneath us.
        lock (_statusShellLock)
        {
            var stations = new JsonObject();
            foreach (var (_, entries) in _cachedEntries)
            {
                var station = new JsonObject();
                var deptsObj = new Dictionary<string, JsonObject>();
                foreach (var entry in entries.Entries)
                {
                    var depts = entry.JobDepartment;

                    foreach (var dept in depts)
                    {
                        var localizedDept = _loc.GetString(dept);
                        if (!deptsObj.TryGetValue(localizedDept, out var obj))
                        {
                            obj = new JsonObject();
                            deptsObj[localizedDept] = obj;
                        }

                        obj[entry.Name] = _loc.GetString(entry.JobTitle);
                    }

                }

                foreach (var (dept, obj) in deptsObj)
                {
                    station[dept] = obj;
                }

                stations[entries.StationName] = station;
            }

            jObject["manifest"] = stations;
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        foreach (var (_, euis) in _openEuis)
        {
            foreach (var (_, eui) in euis)
            {
                eui.Close();
            }
        }

        _openEuis.Clear();
        lock (_statusShellLock)
        {
            _cachedEntries.Clear();
        }
    }

    private void OnRequestCrewManifest(RequestCrewManifestMessage message, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not IPlayerSession sessionCast
            || !_configManager.GetCVar(CCVars.CrewManifestWithoutEntity))
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

    private void OnBoundUiClose(EntityUid uid, CrewManifestViewerComponent component, BoundUIClosedEvent ev)
    {
         var owningStation = _stationSystem.GetOwningStation(uid);
         if (owningStation == null || ev.Session is not IPlayerSession sessionCast)
         {
             return;
         }

         CloseEui(owningStation.Value, sessionCast, uid);
    }

    /// <summary>
    ///     Gets the crew manifest for a given station, along with the name of the station.
    /// </summary>
    /// <param name="station">Entity uid of the station.</param>
    /// <returns>The name and crew manifest entries (unordered) of the station.</returns>
    public (string name, CrewManifestEntries? entries) GetCrewManifest(EntityUid station)
    {
        bool valid;
        CrewManifestEntries? manifest;
        lock (_statusShellLock) // THREAD SAFETY: Cannot modify the cache when status endpoint is busy.
        {
            valid = _cachedEntries.TryGetValue(station, out manifest);
        }

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

        if (!_configManager.GetCVar(CCVars.CrewManifestUnsecure) && component.Unsecure)
        {
            return;
        }

        OpenEui(owningStation.Value, sessionCast, uid);
    }

    /// <summary>
    ///     Opens a crew manifest EUI for a given player.
    /// </summary>
    /// <param name="station">Station that we're displaying the crew manifest for.</param>
    /// <param name="session">The player's session.</param>
    /// <param name="owner">If this EUI should be 'owned' by an entity.</param>
    public void OpenEui(EntityUid station, IPlayerSession session, EntityUid? owner = null)
    {
        if (!HasComp<StationRecordsComponent>(station))
        {
            return;
        }

        if (!_openEuis.TryGetValue(station, out var euis))
        {
            euis = new();
            _openEuis.Add(station, euis);
        }

        if (euis.ContainsKey(session))
        {
            return;
        }

        var eui = new CrewManifestEui(station, owner, this);
        euis.Add(session, eui);

        _euiManager.OpenEui(eui, session);
        eui.StateDirty();
    }

    /// <summary>
    ///     Closes an EUI for a given player.
    /// </summary>
    /// <param name="station">Station that we're displaying the crew manifest for.</param>
    /// <param name="session">The player's session.</param>
    /// <param name="owner">The owner of this EUI, if there was one.</param>
    public void CloseEui(EntityUid station, IPlayerSession session, EntityUid? owner = null)
    {
        if (!HasComp<StationRecordsComponent>(station))
        {
            return;
        }

        if (!_openEuis.TryGetValue(station, out var euis)
            || !euis.TryGetValue(session, out var eui))
        {
            return;
        }

        if (eui.Owner == owner)
        {
            euis.Remove(session);
            eui.Close();
        }

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

        var entries = new CrewManifestEntries()
        {
            StationName = Name(station),
        };

        foreach (var recordObject in iter)
        {
            var record = recordObject.Item2;
            var entry = new CrewManifestEntry(record.Name, record.JobTitle, record.JobIcon,
                record.JobPrototype,
                _jobDepartments[record.JobPrototype].ToList());

            entries.Entries.Add(entry);
        }

        lock (_statusShellLock) // THREAD SAFETY: Cannot modify the cache when status endpoint is busy.
        {
            if (_cachedEntries.ContainsKey(station))
            {

                _cachedEntries[station] = entries;
            }
            else
            {
                _cachedEntries.Add(station, entries);
            }
        }
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class CrewManifestCommand : IConsoleCommand
{
    public string Command => "crewmanifest";
    public string Description => "Opens the crew manifest for the given station.";
    public string Help => $"Usage: {Command} <entity uid>";

    [Dependency] private readonly IEntityManager _entityManager = default!;

    public CrewManifestCommand()
    {
        IoCManager.InjectDependencies(this);
    }

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine($"Invalid argument count.\n{Help}");
            return;
        }

        if (!EntityUid.TryParse(args[0], out var uid))
        {
            shell.WriteLine($"{args[0]} is not a valid entity UID.");
            return;
        }

        if (shell.Player == null || shell.Player is not IPlayerSession session)
        {
            shell.WriteLine("You must run this from a client.");
            return;
        }

        var crewManifestSystem = _entityManager.System<CrewManifestSystem>();

        crewManifestSystem.OpenEui(uid, session);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
        {
            return CompletionResult.Empty;
        }

        var stations = _entityManager
            .System<StationSystem>()
            .Stations
            .Select(station =>
            {
                var meta = _entityManager.GetComponent<MetaDataComponent>(station);

                return new CompletionOption(station.ToString(), meta.EntityName);
            });

        return CompletionResult.FromHintOptions(stations, null);
    }
}
