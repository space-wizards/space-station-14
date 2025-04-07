using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Security.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GenpopLockerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedId;

    [DataField]
    public EntProtoId<GenpopIdCardComponent> IdCardProto = "PrisonerIDCard";
}
