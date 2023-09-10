using System.Linq;
using Content.Server.Administration;
using Content.Server.EUI;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.CrewManifest;
using Content.Shared.GameTicking;
using Content.Shared.StationRecords;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Players;

namespace Content.Server.CrewManifest;

public sealed class CrewManifestSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationRecordsSystem _recordsSystem = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;

    /// <summary>
    ///     Cached crew manifest entries. The alternative is to outright
    ///     rebuild the crew manifest every time the state is requested:
    ///     this is inefficient.
    /// </summary>
    private readonly Dictionary<EntityUid, CrewManifestEntries> _cachedEntries = new();

    private readonly Dictionary<EntityUid, Dictionary<ICommonSession, CrewManifestEui>> _openEuis = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<AfterGeneralRecordCreatedEvent>(AfterGeneralRecordCreated);
        SubscribeLocalEvent<RecordModifiedEvent>(OnRecordModified);
        SubscribeLocalEvent<CrewManifestViewerComponent, BoundUIClosedEvent>(OnBoundUiClose);
        SubscribeLocalEvent<CrewManifestViewerComponent, CrewManifestOpenUiMessage>(OpenEuiFromBui);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeNetworkEvent<RequestCrewManifestMessage>(OnRequestCrewManifest);
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
        _cachedEntries.Clear();
    }

    private void OnRequestCrewManifest(RequestCrewManifestMessage message, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not IPlayerSession sessionCast
            || !_configManager.GetCVar(CCVars.CrewManifestWithoutEntity))
        {
            return;
        }

        OpenEui(GetEntity(message.Id), sessionCast);
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
    public void CloseEui(EntityUid station, ICommonSession session, EntityUid? owner = null)
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

        var entries = new CrewManifestEntries();

        foreach (var recordObject in iter)
        {
            var record = recordObject.Item2;
            var entry = new CrewManifestEntry(record.Name, record.JobTitle, record.JobIcon, record.JobPrototype);

            entries.Entries.Add(entry);
        }

        entries.Entries = entries.Entries.OrderBy(e => e.JobTitle).ThenBy(e => e.Name).ToList();
        _cachedEntries[station] = entries;
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

        if (!NetEntity.TryParse(args[0], out var uidNet) || !_entityManager.TryGetEntity(uidNet, out var uid))
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

        crewManifestSystem.OpenEui(uid.Value, session);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
        {
            return CompletionResult.Empty;
        }

        var stations = _entityManager
            .EntityQuery<StationDataComponent>()
            .Select(stationData =>
            {
                var meta = _entityManager.GetComponent<MetaDataComponent>(stationData.Owner);

                return new CompletionOption(stationData.Owner.ToString(), meta.EntityName);
            });

        return CompletionResult.FromHintOptions(stations, null);
    }
}
