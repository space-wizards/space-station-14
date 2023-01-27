using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing;

[RegisterComponent, NetworkedComponent()]
public sealed partial class MagbootsComponent : Component
{
    [DataField("toggleAction", required: true)]
    public InstantAction ToggleAction = new();

    [ViewVariables]
    public bool On;

    [Serializable, NetSerializable]
    public sealed partial class MagbootsComponentState : ComponentState
    {
        public bool On { get; }

        public MagbootsComponentState(bool @on)
        {
            On = on;
        }
    }
}
