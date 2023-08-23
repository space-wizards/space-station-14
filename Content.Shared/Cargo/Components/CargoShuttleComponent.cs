using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Cargo.Components;

/// <summary>
/// Present on cargo shuttles to provide metadata such as preventing spam calling.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedCargoSystem))]
public sealed partial class CargoShuttleComponent : Component
{
    /// <summary>
    ///     The paper-type prototype to spawn with the order information.
    /// </summary>
    [DataField("printerOutput", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PrinterOutput = "PaperCargoInvoice";

}
