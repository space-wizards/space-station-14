using Content.Server.StationEvents.Events;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Used an event that gifts the station with certain cargo
/// </summary>
[RegisterComponent, Access(typeof(CargoGiftsRule)), AutoGenerateComponentPause]
public sealed partial class CargoGiftsRuleComponent : Component
{
    /// <summary>
    /// The base announcement string (which then incorporates the strings below)
    /// </summary>
    [DataField]
    public LocId Announce = "cargo-gifts-event-announcement";

    /// <summary>
    /// What is being sent
    /// </summary>
    [DataField]
    public LocId Description = "cargo-gift-default-description";

    /// <summary>
    /// Sender of the gifts
    /// </summary>
    [DataField]
    public LocId Sender = "cargo-gift-default-sender";

    /// <summary>
    /// Destination of the gifts (who they get sent to on the station)
    /// </summary>
    [DataField]
    public LocId Dest = "cargo-gift-default-dest";

    /// <summary>
    /// Account the gifts are deposited into
    /// </summary>
    [DataField]
    public ProtoId<CargoAccountPrototype> Account = "Cargo";

    /// <summary>
    /// Cargo that you would like gifted to the station, with the quantity for each
    /// Use Ids from cargoProduct Prototypes
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<CargoProductPrototype>, int> Gifts = new();

    /// <summary>
    /// How much space (minimum) you want to leave in the order database for supply to actually do their work
    /// </summary>
    [DataField]
    public int OrderSpaceToLeave = 5;

    /// <summary>
    /// Time we consider next lot of gifts at (if supply is overflowing with orders)
    /// </summary>
    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ConsiderNextGiftsAt;
}
