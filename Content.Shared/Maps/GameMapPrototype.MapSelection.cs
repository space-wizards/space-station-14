namespace Content.Shared.Maps;

public sealed partial class GameMapPrototype
{
    /// <summary>
    /// Controls if the map can be used as a fallback if no maps are eligible.
    /// </summary>
    [DataField("fallback")]
    public bool Fallback { get; private set; }

    /// <summary>
    /// Minimum players for the given map.
    /// </summary>
    [DataField("minPlayers", required: true)]
    public uint MinPlayers { get; private set; }

    /// <summary>
    /// Maximum players for the given map.
    /// </summary>
    [DataField("maxPlayers")]
    public uint MaxPlayers { get; private set; } = uint.MaxValue;

    [DataField("conditions")] private  List<GameMapCondition> _conditions = new();

    /// <summary>
    /// The game map conditions that must be fulfilled for this map to be selectable.
    /// </summary>
    public IReadOnlyList<GameMapCondition> Conditions => _conditions;
}
