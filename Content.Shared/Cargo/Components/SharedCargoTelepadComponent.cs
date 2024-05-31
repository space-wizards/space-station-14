using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Cargo.Components;

/// <summary>
/// Handles teleporting in requested cargo after the specified delay.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedCargoSystem))]
public sealed partial class CargoTelepadComponent : Component
{
    [DataField]
    public List<CargoOrderData> CurrentOrders = new();

    /// <summary>
    /// The actual amount of time it takes to teleport from the telepad
    /// </summary>
    [DataField("delay"), ViewVariables(VVAccess.ReadWrite)]
    public float Delay = 5f;

    /// <summary>
    /// How much time we've accumulated until next teleport.
    /// </summary>
    [DataField("accumulator"), ViewVariables(VVAccess.ReadWrite)]
    public float Accumulator;

    [DataField("currentState")]
    public CargoTelepadState CurrentState = CargoTelepadState.Unpowered;

    [DataField("teleportSound")]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Machines/phasein.ogg");

    /// <summary>
    ///     The paper-type prototype to spawn with the order information.
    /// </summary>
    [DataField("printerOutput", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string PrinterOutput = "PaperCargoInvoice";

    [DataField("receiverPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string ReceiverPort = "OrderReceiver";
}
