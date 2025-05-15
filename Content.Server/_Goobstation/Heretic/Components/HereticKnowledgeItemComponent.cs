using Content.Server.Heretic.EntitySystems;

namespace Content.Server.Heretic.Components;

[RegisterComponent, Access(typeof(HereticKnowledgeItemSystem))]
public sealed partial class HereticKnowledgeItemComponent : Component
{
    [DataField]
    public bool Spent;

    [DataField]
    public float PointGain = 1f;

    [DataField]
    public float UseTimeSeconds = 10f;
}
