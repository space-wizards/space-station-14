using Robust.Shared.GameStates;

namespace Content.Shared.Access.Components;

/// <summary>
///     Stores allowed directions necessary to "use" an entity
///     and allows checking if something or somebody is activating it from an allowed direction
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class DirectionalAccessComponent : Component
{
    /// <summary>
    /// Whether or not the DirectionalAccess is enabled.
    /// If not, it will always let people through.
    /// </summary>
    [DataField("enabled")]
    public bool Enabled = true;

    /// <summary>
    ///     List of directions to check allowed against. For an access check to pass
    ///     there has to be an access list that is a subset of the access in the checking list.
    /// </summary>
    /// Possibly List of strings, datatype is too much
    [DataField("allowedDirections")]
    public List<CardinalDirections> DirectionsList = new();
}

/// <summary>
/// Cardinal direction (N, E, S, W) enum for possible directions
/// </summary>
public enum CardinalDirections
{
    North,
    East,
    South,
    West
}
