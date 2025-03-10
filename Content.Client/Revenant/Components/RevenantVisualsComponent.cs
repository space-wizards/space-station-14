using Robust.Client.Graphics;

namespace Content.Client.Revenant.Components;

[RegisterComponent]
public sealed partial class RevenantVisualsComponent : Component
{
    [ViewVariables]
    public RSI.StateId State = "idle";

    [ViewVariables]
    public RSI.StateId CorporealState = "active";

    [ViewVariables]
    public RSI.StateId StunnedState = "stunned";

    [ViewVariables]
    public RSI.StateId HarvestingState = "harvesting";
}
