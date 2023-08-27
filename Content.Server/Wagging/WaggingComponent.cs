using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Wagging;

[RegisterComponent, NetworkedComponent]
[Access(typeof(WaggingSystem))]
public sealed partial class WaggingComponent : Component
{
    [DataField("action", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string Action = "ToggleWagging";

    [ViewVariables]
    public bool Wagging = false;
}
