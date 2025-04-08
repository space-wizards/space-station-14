using Content.Server.Heretic.EntitySystems;

namespace Content.Server.Heretic.Components;

[RegisterComponent, Access(typeof(HereticKnowledgeItemSystem))]
public sealed partial class HereticKnowledgeItemComponent : Component
{
    [DataField]
    public bool Spent;
}
