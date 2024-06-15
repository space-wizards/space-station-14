using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Clothing;

[RegisterComponent, NetworkedComponent(), AutoGenerateComponentState]
[Access(typeof(SharedMagbootsSystem))]
public sealed partial class MagbootsComponent : Component
{
    [DataField]
    public EntProtoId ToggleAction = "ActionToggleMagboots";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;

    [DataField("on"), AutoNetworkedField]
    public bool On;

    [DataField]
    public ProtoId<AlertPrototype> MagbootsAlert = "Magboots";
}
