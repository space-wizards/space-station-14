namespace Content.Server.Maps;

public sealed partial class GameMapPrototype
{
    /// <summary>
    /// Controls if the map can be used as a fallback if no maps are eligible.
    /// </summary>
    [DataField("fallback")] public readonly bool Fallback;

    /// <summary>
    /// Controls if the map can be voted for.
    /// </summary>
    [DataField("votable")] public readonly bool Votable;

    /// <summary>
    /// Minimum players for the given map.
    /// </summary>
    [DataField("minPlayers", required: true)] public readonly uint MinPlayers;

    /// <summary>
    /// Maximum players for the given map.
    /// </summary>
    [DataField("maxPlayers")] public readonly uint MaxPlayers = uint.MaxValue;

    [DataField("conditions")] private readonly List<GameMapCondition> _conditions = new();

    /// <summary>
    /// The game map conditions that must be fulfilled for this map to be selectable.
    /// </summary>
    public IReadOnlyList<GameMapCondition> Conditions => _conditions;

    /// <summary>
    /// How far apart the boundary of other maps should be from the center of this map.
    /// </summary>
    /// <remarks>
    /// This is summed with the map separation for the other map, effectively making it two circles touching.
    /// </remarks>
    public float MapSeparation = 300.0f;

}
