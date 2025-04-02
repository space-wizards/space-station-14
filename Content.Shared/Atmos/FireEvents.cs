namespace Content.Shared.Atmos;

// NOTE: These components are currently not raised on the client, only on the server.

/// <summary>
/// A flammable entity has been extinguished.
/// </summary>
[ByRefEvent]
public struct FlammableExtinguished;

/// <summary>
/// A flammable entity has been ignited.
/// </summary>
[ByRefEvent]
public struct FlammableIgnited;
