using System.Collections.Generic;

namespace Content.Server.Maps;

/// <summary>
/// Manages which station map will be used for the next round.
/// </summary>
public interface IGameMapManager
{
    void Initialize();

    /// <summary>
    /// Returns all maps eligible to be played right now.
    /// </summary>
    /// <returns>enumerator of map prototypes</returns>
    IEnumerable<GameMapPrototype> CurrentlyEligibleMaps();

    /// <summary>
    /// Returns all maps that can be voted for.
    /// </summary>
    /// <returns>enumerator of map prototypes</returns>
    IEnumerable<GameMapPrototype> AllVotableMaps();

    /// <summary>
    /// Returns all maps.
    /// </summary>
    /// <returns>enumerator of map prototypes</returns>
    IEnumerable<GameMapPrototype> AllMaps();

    /// <summary>
    /// Attempts to select the given map.
    /// </summary>
    /// <param name="gameMap">map prototype</param>
    /// <returns>success or failure</returns>
    bool TrySelectMap(string gameMap);

    /// <summary>
    /// Forces the given map, making sure the game map manager won't reselect if conditions are no longer met at round restart.
    /// </summary>
    /// <param name="gameMap">map prototype</param>
    /// <returns>success or failure</returns>
    void ForceSelectMap(string gameMap);

    /// <summary>
    /// Selects a random map.
    /// </summary>
    void SelectRandomMap();

    /// <summary>
    /// Gets the currently selected map, without double-checking if it can be used.
    /// </summary>
    /// <returns>selected map</returns>
    GameMapPrototype GetSelectedMap();

    /// <summary>
    /// Gets the currently selected map, double-checking if it can be used.
    /// </summary>
    /// <returns>selected map</returns>
    GameMapPrototype GetSelectedMapChecked(bool loud = false);

    /// <summary>
    /// Checks if the given map exists
    /// </summary>
    /// <param name="gameMap">name of the map</param>
    /// <returns>existence</returns>
    bool CheckMapExists(string gameMap);

    public string GenerateMapName(GameMapPrototype gameMap);
}
