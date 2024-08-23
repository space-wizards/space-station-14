using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Misc;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(COSharedTelekinesisSystem))]
public sealed partial class COTelekinesisComponent : Component
{
    [ViewVariables, DataField, AutoNetworkedField]

    public EntProtoId ActionProto = "COActionTelekinesis";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;
}
