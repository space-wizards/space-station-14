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

    /// <summary>
    /// Median number of players for the map.
    /// </summary>
    public uint MedianPlayers => (MinPlayers + MaxPlayers) / 2;

    [DataField("conditions")] private readonly List<GameMapCondition> _conditions = new();

    /// <summary>
    /// The game map conditions that must be fulfilled for this map to be selectable.
    /// </summary>
    public IReadOnlyList<GameMapCondition> Conditions => _conditions;

    /// <summary>
    /// Whether or not this map can be duplicated when trying to meet a pop demand.
    /// This is still constrained by MaxPartners.
    /// </summary>
    [DataField("canDuplicate")] public readonly bool CanDuplicate;

    /// <summary>
    /// The maximum number of maps that this map can be loaded alongside.
    /// </summary>
    [DataField("maximumPartners")] public readonly int MaximumPartners = 1;
}
