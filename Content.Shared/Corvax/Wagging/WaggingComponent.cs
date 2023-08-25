using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Corvax.Wagging;

[RegisterComponent, NetworkedComponent]
public sealed partial class WaggingComponent : Component
{
    [DataField("toggleAction", required: true)]
    public InstantAction ToggleAction = new();

    [ViewVariables] public bool Wagging = false;
}
