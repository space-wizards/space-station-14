using Content.Shared.StationRecords;

namespace Content.Server.Delivery;

/// <summary>
/// Raised on a delivery during recipient assignment so special delivery types
/// can choose a specific station record instead of the default random one.
/// </summary>
[ByRefEvent]
public struct DeliverySelectRecipientEvent
{
    public readonly EntityUid Station;

    /// <summary>
    /// The chosen recipient record. Leave null to fall back to a random station record.
    /// </summary>
    public GeneralStationRecord? Recipient;

    /// <summary>
    /// Set to cancel and delete the delivery (e.g. no valid recipient could be found).
    /// </summary>
    public bool Cancelled;

    public DeliverySelectRecipientEvent(EntityUid station)
    {
        Station = station;
    }
}
