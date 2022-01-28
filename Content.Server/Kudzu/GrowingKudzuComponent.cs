using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Kudzu;

[RegisterComponent]
public class GrowingKudzuComponent : Component
{
    public override string Name => "GrowingKudzu";

    [DataField("growthLevel")]
    public int GrowthLevel = 1;

    [DataField("growthTickSkipChance")]
    public float GrowthTickSkipChange = 0.0f;
}
