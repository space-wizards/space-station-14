using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.HealthConditions.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HealthConditionManagerComponent : Component
{
    public const string ContainerId = "Afflictions";

    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId, EntityUid> ContainedConditionEntities = new ();
}
