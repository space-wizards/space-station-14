using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Chat.Managers;
using Content.Shared.CCVar;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Maps;

public class GameMapManager : IGameMapManager
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    private GameMapPrototype? _currentMap;
    private bool _currentMapForced;

    public void Initialize()
    {
        _configurationManager.OnValueChanged(CCVars.GameMap, SetMap, true);
        _configurationManager.OnValueChanged(CCVars.GameMapForced, value => _currentMapForced = value, true);
    }

    private void SetMap(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        if (TryLookupMap(value, out var map))
            _currentMap = map;
        else
            throw new ArgumentException($"Unknown map prototype {value} was selected!");
    }

    public IEnumerable<GameMapPrototype> CurrentlyEligibleMaps()
    {
        var maps = AllVotableMaps().Where(IsMapEligible).ToArray();

        return maps.Length == 0 ? AllMaps().Where(x => x.Fallback) : maps;
    }

    public IEnumerable<GameMapPrototype> AllVotableMaps()
    {
        return _prototypeManager.EnumeratePrototypes<GameMapPrototype>().Where(x => x.Votable);
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
        return true;

    }

    public void ForceSelectMap(string gameMap)
    {
        if (!TryLookupMap(gameMap, out var map))
            throw new ArgumentException($"The map \"{gameMap}\" is invalid!");
        _currentMap = map;
        _currentMapForced = true;
    }

    public void SelectRandomMap()
    {
        var maps = CurrentlyEligibleMaps().ToList();
        _random.Shuffle(maps);
        _currentMap = maps[0];
        _currentMapForced = false;
    }

    public GameMapPrototype? GetSelectedMap()
    {
        return _currentMap;
    }

    public GameMapPrototype? GetSelectedMapChecked(bool loud = false)
    {
        var map = GetSelectedMap();

        if (map != null && !_currentMapForced && !IsMapEligible(map))
        {
            var oldMap = map.MapName;
            SelectRandomMap();
            if (loud)
            {
                _chatManager.DispatchServerAnnouncement(
                    Loc.GetString("gamemap-could-not-use-map-error",
                        ("oldMap", oldMap), ("newMap", map.MapName)
                    ));
            }
        }

        return map;
    }

    public bool CheckMapExists(string gameMap)
    {
        return TryLookupMap(gameMap, out _);
    }

    private bool IsMapEligible(GameMapPrototype? map)
    {
        return map != null && map.MaxPlayers >= _playerManager.PlayerCount && map.MinPlayers <= _playerManager.PlayerCount;
    }

    private bool TryLookupMap(string gameMap, [NotNullWhen(true)] out GameMapPrototype? map)
    {
        return _prototypeManager.TryIndex(gameMap, out map);
    }

    public string GenerateMapName(GameMapPrototype gameMap)
    {
        if (gameMap.NameGenerator is not null && gameMap.MapNameTemplate is not null)
            return gameMap.NameGenerator.FormatName(gameMap.MapNameTemplate);
        else
            return gameMap.MapName;
    }
}
