using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Wagging;

[RegisterComponent, NetworkedComponent]
[Access(typeof(WaggingSystem))]
public sealed partial class WaggingComponent : Component
{
    [DataField("action", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Action = "ActionToggleWagging";

    [DataField("invisibleWallActionEntity")]
    public EntityUid? ActionEntity;

    [ViewVariables]
    public bool Wagging = false;
}
