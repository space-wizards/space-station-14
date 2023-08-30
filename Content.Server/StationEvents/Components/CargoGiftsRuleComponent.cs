using Content.Server.StationEvents.Events;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Used an event that gifts the station with certian cargo
/// </summary>
[RegisterComponent, Access(typeof(CargoGiftsRule))]
public sealed partial class CargoGiftsRuleComponent : Component
{
    /// <summary>
    /// The base announcement string (which then incorporates the strings below)
    /// </summary>
    [DataField("announce"), ViewVariables(VVAccess.ReadWrite)]
    public string Announce = "cargo-gifts-event-announcement";

    /// <summary>
    /// What is being sent
    /// </summary>
    [DataField("description"), ViewVariables(VVAccess.ReadWrite)]
    public string Description = "cargo-gift-default-description";

    /// <summary>
    /// Sender of the gifts
    /// </summary>
    [DataField("sender"), ViewVariables(VVAccess.ReadWrite)]
    public string Sender = "cargo-gift-default-sender";

    /// <summary>
    /// Destination of the gifts (who they get sent to on the station)
    /// </summary>
    [DataField("dest"), ViewVariables(VVAccess.ReadWrite)]
    public string Dest = "cargo-gift-default-dest";

    /// <summary>
    /// Cargo that you would like gifted to the station, with the quantity for each
    /// Use Ids from cargoProduct Prototypes
    /// </summary>
    [DataField("gifts", required: true, customTypeSerializer:typeof(PrototypeIdDictionarySerializer<int, CargoProductPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, int> Gifts = new();

    /// <summary>
    /// How much space (minimum) you want to leave in the order database for supply to actually do their work
    /// </summary>
    [DataField("orderSpaceToLeave"), ViewVariables(VVAccess.ReadWrite)]
    public int OrderSpaceToLeave = 5;

    /// <summary>
    /// Time until we consider next lot of gifts (if supply is overflowing with orders)
    /// </summary>
    [DataField("timeUntilNextGifts"), ViewVariables(VVAccess.ReadWrite)]
    public float TimeUntilNextGifts = 10.0f;
}
