using Content.Shared.Cargo;

namespace Content.Server.Cargo.Components;

/// <summary>
/// Stores all of cargo orders for a particular station.
/// </summary>
[RegisterComponent]
public sealed class StationCargoOrderDatabaseComponent : Component
{
    /// <summary>
    /// Maximum amount of orders a station is allowed, approved or not.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("capacity")]
    public int Capacity = 20;

    [ViewVariables(VVAccess.ReadWrite), DataField("orders")]
    public Dictionary<int, CargoOrderData> Orders = new();

    /// <summary>
    /// Tracks the next order index available.
    /// </summary>
    public int Index;

    [ViewVariables, DataField("cargoShuttleProto")]
    public string? CargoShuttleProto = "CargoShuttle";

    /// <summary>
    /// The cargo shuttle assigned to this station.
    /// </summary>
    [ViewVariables, DataField("shuttle")]
    public EntityUid? Shuttle;
}
