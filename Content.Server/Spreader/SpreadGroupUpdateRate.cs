namespace Content.Server.Spreader;

/// <summary>
/// Raised every tick to determine how many updates a particular spreading node group is allowed.
/// </summary>
[ByRefEvent]
public record struct SpreadGroupUpdateRate(string Name, int UpdatesPerSecond = 16);
