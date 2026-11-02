namespace Content.Shared.CF.Factions;

/// <summary>
/// How two CarpFinder factions treat each other.
/// </summary>
public enum CFAlliance : byte
{
    /// <summary>Same side. They do not damage each other and do not hunt each other.</summary>
    Party,

    /// <summary>Enemies. They can damage each other, and mobs will target them.</summary>
    Opposition,

    /// <summary>No quarrel. Mobs do not target them. Attacks are still allowed.</summary>
    Neutral,
}
