using Content.Server.Atmos.Piping.Binary.EntitySystems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Atmos.Piping.Binary.Components;

[RegisterComponent, Access(typeof(SignalControlledValveSystem))]
public sealed partial class SignalControlledValveComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>)))]
    public string OpenPort = "Open";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>)))]
    public string ClosePort = "Close";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>)))]
    public string TogglePort = "Toggle";
}
