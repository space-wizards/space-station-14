using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Clothing;

[RegisterComponent, NetworkedComponent(), AutoGenerateComponentState]
[Access(typeof(SharedMagbootsSystem))]
public sealed partial class MagbootsComponent : Component
{
    [DataField("toggleActionId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>), required: true)]
    public string? ToggleActionId;

    [DataField("toggleAction")]
    public EntityUid? ToggleAction;

    [DataField("on"), AutoNetworkedField]
    public bool On;
}
