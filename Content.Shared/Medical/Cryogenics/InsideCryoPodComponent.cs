using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Cryogenics;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class InsideCryoPodComponent: Component
{
    [ViewVariables]
    [DataField("previousOffset")]
    public Vector2 PreviousOffset { get; set; } = new(0, 0);
}
