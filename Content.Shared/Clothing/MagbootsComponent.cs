using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing;

[RegisterComponent, NetworkedComponent(), AutoGenerateComponentState]
[Access(typeof(SharedMagbootsSystem))]
public sealed partial class MagbootsComponent : Component
{
    [DataField("toggleAction", required: true)]
    public InstantAction ToggleAction = new();

    [DataField("on"), AutoNetworkedField]
    public bool On;
}
