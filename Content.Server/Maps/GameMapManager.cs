using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Maps;

public sealed class GameMapManager : IGameMapManager
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IResourceManager _resMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    private readonly Queue<string> _previousMaps = new();
    [ViewVariables(VVAccess.ReadOnly)]
    private GameMapPrototype? _configSelectedMap;
    [ViewVariables(VVAccess.ReadOnly)]
    private GameMapPrototype? _selectedMap; // Don't change this value during a round!
    [ViewVariables(VVAccess.ReadOnly)]
    private bool _mapRotationEnabled;
    [ViewVariables(VVAccess.ReadOnly)]
    private int _mapQueueDepth = 1;

    private ISawmill _log = default!;

    public void Initialize()
    {
        _log = Logger.GetSawmill("mapsel");

        _configurationManager.OnValueChanged(CCVars.GameMap, value =>
        {
            if (TryLookupMap(value, out GameMapPrototype? map))
            {
                _configSelectedMap = map;
                return;
            }

            if (string.IsNullOrEmpty(value))
            {
                _configSelectedMap = default!;
                return;
            }

            if (_configurationManager.GetCVar<bool>(CCVars.UsePersistence))
            {
                var startMap = _configurationManager.GetCVar<string>(CCVars.PersistenceMap);
                _configSelectedMap = _prototypeManager.Index<GameMapPrototype>(startMap);

                var mapPath = new ResPath(value);
                if (_resMan.UserData.Exists(mapPath))
                {
                    _configSelectedMap = _configSelectedMap.Persistence(mapPath);
                    _log.Info($"Using persistence map from {value}");
                    return;
                }

                // persistence save path doesn't exist so we just use the start map
                _log.Warning($"Using persistence start map {startMap} as {value} doesn't exist");
                return;
            }

            _log.Error($"Unknown map prototype {value} was selected!");
        }, true);
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
        var poolPrototype = _entityManager.System<GameTicker>().Preset?.MapPool ??
                   _configurationManager.GetCVar(CCVars.GameMapPool);

        if (_prototypeManager.TryIndex<GameMapPoolPrototype>(poolPrototype, out var pool))
        {
            foreach (var map in pool.Maps)
            {
                if (!_prototypeManager.TryIndex<GameMapPrototype>(map, out var mapProto))
                {
                    _log.Error($"Couldn't index map {map} in pool {poolPrototype}");
                    continue;
                }

                yield return mapProto;
            }
        }
        else
        {
            throw new Exception($"Could not index map pool prototype {poolPrototype}!");
        }
    }

    public IEnumerable<GameMapPrototype> AllMaps()
    {
        return _prototypeManager.EnumeratePrototypes<GameMapPrototype>();
    }

    public GameMapPrototype? GetSelectedMap()
    {
        return _configSelectedMap ?? _selectedMap;
    }

    public void ClearSelectedMap()
    {
        _selectedMap = default!;
    }

    public bool TrySelectMapIfEligible(string gameMap)
    {
        if (!TryLookupMap(gameMap, out var map) || !IsMapEligible(map))
            return false;
        _selectedMap = map;
        return true;
    }

    public void SelectMap(string gameMap)
    {
        if (!TryLookupMap(gameMap, out var map))
            throw new ArgumentException($"The map \"{gameMap}\" is invalid!");
        _selectedMap = map;
    }

    public void SelectMapRandom()
    {
        var maps = CurrentlyEligibleMaps().ToList();
        _selectedMap = _random.Pick(maps);
    }

    public void SelectMapFromRotationQueue(bool markAsPlayed = false)
    {
        var map = GetFirstInRotationQueue();

        _selectedMap = map;

        if (markAsPlayed)
            EnqueueMap(map.ID);
    }

    public void SelectMapByConfigRules()
    {
        if (_mapRotationEnabled)
        {
            _log.Info("selecting the next map from the rotation queue");
            SelectMapFromRotationQueue(true);
        }
        else
        {
            _log.Info("selecting a random map");
            SelectMapRandom();
        }
    }

    public bool CheckMapExists(string gameMap)
    {
        return TryLookupMap(gameMap, out _);
    }

    private bool IsMapEligible(GameMapPrototype map)
    {
        return map.MaxPlayers >= _playerManager.PlayerCount &&
               map.MinPlayers <= _playerManager.PlayerCount &&
               map.Conditions.All(x => x.Check(map)) &&
               _entityManager.System<GameTicker>().IsMapEligible(map);
    }

    private bool TryLookupMap(string gameMap, [NotNullWhen(true)] out GameMapPrototype? map)
    {
        return _prototypeManager.TryIndex(gameMap, out map);
    }

    private int GetMapRotationQueuePriority(string gameMapProtoName)
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

    private GameMapPrototype GetFirstInRotationQueue()
    {
        _log.Info($"map queue: {string.Join(", ", _previousMaps)}");

        var eligible = CurrentlyEligibleMaps()
            .Select(x => (proto: x, weight: GetMapRotationQueuePriority(x.ID)))
            .OrderByDescending(x => x.weight)
            .ToArray();

        _log.Info($"eligible queue: {string.Join(", ", eligible.Select(x => (x.proto.ID, x.weight)))}");

        // YML "should" be configured with at least one fallback map
        Debug.Assert(eligible.Length != 0, $"couldn't select a map with {nameof(GetFirstInRotationQueue)}()! No eligible maps and no fallback maps!");

        var weight = eligible[0].weight;
        return eligible.Where(x => x.Item2 == weight)
            .MinBy(x => x.proto.ID)
            .proto;
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
