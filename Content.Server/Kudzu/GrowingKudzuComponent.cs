using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.Kudzu;

[RegisterComponent]
public class GrowingKudzuComponent : Component
{
    public override string Name => "GrowingKudzu";

    [ViewVariables]
    public int GrowthLevel = 1;
}
