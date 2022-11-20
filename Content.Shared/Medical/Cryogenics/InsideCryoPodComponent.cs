using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Cryogenics;

[RegisterComponent]
[NetworkedComponent]
public sealed class InsideCryoPodComponent: Component
{
    [ViewVariables]
    [DataField("holder")]
    public EntityUid Holder;

    [ViewVariables]
    [DataField("previousOffset")]
    public Vector2 PreviousOffset { get; set; } = new(0, 0);
}
