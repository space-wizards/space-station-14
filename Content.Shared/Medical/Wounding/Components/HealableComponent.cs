using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounding.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Wounding.Components;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(WoundSystem))]
public sealed partial class HealableComponent : Component
{

    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextUpdate = default;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Modifier = 1.0;

}
