using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Chat.Managers;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Maps;

public sealed class GameMapManager : IGameMapManager
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    [ViewVariables]
    private readonly Queue<string> _previousMaps = new();
    [ViewVariables]
    private GameMapPrototype _currentMap = default!;
    [ViewVariables]
    private bool _currentMapForced;
    [ViewVariables]
    private bool _mapRotationEnabled;
    [ViewVariables]
    private int _mapQueueDepth = 1;

    public void Initialize()
    {
        _configurationManager.OnValueChanged(CCVars.GameMap, value =>
        {
            if (TryLookupMap(value, out var map))
                _currentMap = map;
            else
                throw new ArgumentException($"Unknown map prototype {value} was selected!");
        }, true);
        _configurationManager.OnValueChanged(CCVars.GameMapForced, value => _currentMapForced = value, true);
        _configurationManager.OnValueChanged(CCVars.GameMapRotation, value => _mapRotationEnabled = value, true);
        _configurationManager.OnValueChanged(CCVars.GameMapMemoryDepth, value =>
        {
            _mapQueueDepth = value;
            // Drain excess.
            while (_previousMaps.Count > _mapQueueDepth)
            {
                _previousMaps.Dequeue();
            }
        }, true);

        var maps = AllVotableMaps().ToArray();
        _random.Shuffle(maps);
        foreach (var map in maps)
        {
            if (_previousMaps.Count >= _mapQueueDepth)
                break;
            _previousMaps.Enqueue(map.ID);
        }
    }

    public IEnumerable<GameMapPrototype> CurrentlyEligibleMaps()
    {
        var maps = AllVotableMaps().Where(IsMapEligible).ToArray();

        return maps.Length == 0 ? AllMaps().Where(x => x.Fallback) : maps;
    }

    public IEnumerable<GameMapPrototype> AllVotableMaps()
    {
        if (_prototypeManager.TryIndex<GameMapPoolPrototype>(_configurationManager.GetCVar(CCVars.GameMapPool), out var pool))
        {
            foreach (var map in pool.Maps)
            {
                if (!_prototypeManager.TryIndex<GameMapPrototype>(map, out var mapProto))
                {
                    Logger.Error("Couldn't index map " + map + " in pool " + pool.ID);
                    continue;
                }

                yield return mapProto;
            }
        } else
        {
            throw new Exception("Could not index map pool prototype " + _configurationManager.GetCVar(CCVars.GameMapPool) + "!");
        }
    }

    public IEnumerable<GameMapPrototype> AllMaps()
    {
        return _prototypeManager.EnumeratePrototypes<GameMapPrototype>();
    }

    public bool TrySelectMap(string gameMap)
    {
        if (!TryLookupMap(gameMap, out var map) || !IsMapEligible(map)) return false;

        _currentMap = map;
        _currentMapForced = false;
        var ticker = EntitySystem.Get<GameTicking.GameTicker>();
        ticker.UpdateInfoText();
        return true;

    }

    public void ForceSelectMap(string gameMap)
    {
        if (!TryLookupMap(gameMap, out var map))
            throw new ArgumentException($"The map \"{gameMap}\" is invalid!");
        _currentMap = map;
        _currentMapForced = true;
        var ticker = EntitySystem.Get<GameTicking.GameTicker>();
        ticker.UpdateInfoText();
    }

    public void SelectRandomMap()
    {
        var maps = CurrentlyEligibleMaps().ToList();
        _currentMap = _random.Pick(maps);
        _currentMapForced = false;
        var ticker = EntitySystem.Get<GameTicking.GameTicker>();
        ticker.UpdateInfoText();
    }

    public GameMapPrototype GetSelectedMap()
    {
        if (!_mapRotationEnabled || _currentMapForced)
            return _currentMap;
        return SelectMapInQueue() ?? CurrentlyEligibleMaps().First();
    }

    public GameMapPrototype GetSelectedMapChecked(bool loud = false, bool markAsPlayed = false)
    {
        if (!_currentMapForced && !IsMapEligible(GetSelectedMap()))
        {
            var oldMap = GetSelectedMap().MapName;
            SelectRandomMap();
            if (loud)
            {
                _chatManager.DispatchServerAnnouncement(
                    Loc.GetString("gamemap-could-not-use-map-error",
                        ("oldMap", oldMap), ("newMap", GetSelectedMap().MapName)
                    ));
            }
        }

        var map = GetSelectedMap();

        if (markAsPlayed)
            EnqueueMap(map.ID);
        return map;
    }

    public bool CheckMapExists(string gameMap)
    {
        return TryLookupMap(gameMap, out _);
    }

    private bool IsMapEligible(GameMapPrototype map)
    {
        return map.MaxPlayers >= _playerManager.PlayerCount &&
               map.MinPlayers <= _playerManager.PlayerCount &&
               map.Conditions.All(x => x.Check(map));
    }

    private bool TryLookupMap(string gameMap, [NotNullWhen(true)] out GameMapPrototype? map)
    {
        return _prototypeManager.TryIndex(gameMap, out map);
    }

    public int GetMapQueuePriority(string gameMapProtoName)
    {
        var i = 0;
        foreach (var map in _previousMaps.Reverse())
        {
            if (map == gameMapProtoName)
                return i;
            i++;
        }

        return _mapQueueDepth;
    }

    public GameMapPrototype? SelectMapInQueue()
    {
        Logger.InfoS("mapsel", string.Join(", ", _previousMaps));
        var eligible = CurrentlyEligibleMaps()
            .Select(x => (proto: x, weight: GetMapQueuePriority(x.ID)))
            .OrderByDescending(x => x.weight).ToArray();
        Logger.InfoS("mapsel", string.Join(", ", eligible.Select(x => (x.proto.ID, x.weight))));
        if (eligible.Length is 0)
            return null;

        var weight = eligible[0].weight;
        return eligible.Where(x => x.Item2 == weight).OrderBy(x => x.proto.ID).First().proto;
    }

    private void EnqueueMap(string mapProtoName)
    {
        _previousMaps.Enqueue(mapProtoName);
        while (_previousMaps.Count > _mapQueueDepth)
        {
            _previousMaps.Dequeue();
        }
    }
}
