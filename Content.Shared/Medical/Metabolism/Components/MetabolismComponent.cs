using Content.Shared.Damage;
using Content.Shared.Medical.Metabolism.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Metabolism.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MetabolismComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<MetabolismTypePrototype> MetabolismType;
    [DataField(required: true)]
    public float BaseMultiplier = 1;
}
