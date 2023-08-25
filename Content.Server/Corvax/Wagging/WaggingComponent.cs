using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameStates;

namespace Content.Server.Corvax.Wagging;

[RegisterComponent, NetworkedComponent]
[Access(typeof(WaggingSystem))]
public sealed partial class WaggingComponent : Component
{
    [DataField("toggleAction", required: true)]
    public InstantAction ToggleAction = new();

    [ViewVariables]
    public bool Wagging = false;
}
