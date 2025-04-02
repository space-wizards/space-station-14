using Content.Shared.Inventory;

namespace Content.Shared.Atmos;

// NOTE: These components are currently not raised on the client, only on the server.

/// <summary>
/// An entity has had an existing effect applied to it.
/// </summary>
/// <remarks>
/// This does not necessarily mean the effect is strong enough to fully extinguish the entity in one go.
/// </remarks>
[ByRefEvent]
public struct ExtinguishEvent : IInventoryRelayEvent
{
    /// <summary>
    /// Amount of firestacks changed. Should be a negative number.
    /// </summary>
    public float FireStacksAdjustment;

    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.WITHOUT_POCKET;
}

/// <summary>
/// A flammable entity has been extinguished.
/// </summary>
/// <seealso cref="ExtinguishEvent"/>
[ByRefEvent]
public struct FlammableExtinguished;

/// <summary>
/// A flammable entity has been ignited.
/// </summary>
[ByRefEvent]
public struct FlammableIgnited;
