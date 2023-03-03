using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Cargo.Components;

/// <summary>
/// Present on cargo shuttles to provide metadata such as preventing spam calling.
/// </summary>
[RegisterComponent, Access(typeof(SharedCargoSystem))]
public sealed class CargoShuttleComponent : Component
{
    /// <summary>
    /// The assigned station for this cargo shuttle.
    /// </summary>
    [DataField("station")]
    public EntityUid? Station;

    /// <summary>
    ///     The paper-type prototype to spawn with the order information.
    /// </summary>
    [DataField("printerOutput", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PrinterOutput = "PaperCargoInvoice";

}
