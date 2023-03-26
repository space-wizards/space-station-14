using Content.Server.Atmos.Piping.Binary.EntitySystems;
using Content.Shared.MachineLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Atmos.Piping.Binary.Components;

[RegisterComponent, Access(typeof(SignalControlledValveSystem))]
public sealed class SignalControlledValveComponent : Component
{
    [DataField("openPort", customTypeSerializer: typeof(PrototypeIdSerializer<ReceiverPortPrototype>))]
    public string OpenPort = "Open";

    [DataField("closePort", customTypeSerializer: typeof(PrototypeIdSerializer<ReceiverPortPrototype>))]
    public string ClosePort = "Close";

    [DataField("togglePort", customTypeSerializer: typeof(PrototypeIdSerializer<ReceiverPortPrototype>))]
    public string TogglePort = "Toggle";
}
