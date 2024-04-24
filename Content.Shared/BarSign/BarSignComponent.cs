using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.BarSign;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class BarSignComponent : Component
{
    [DataField, AutoNetworkedField] public ProtoId<BarSignPrototype>? Current;
}
