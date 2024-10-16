using Content.Server.Heretic;
using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

[RegisterComponent]
public sealed partial class HereticKnowledgeConditionComponent : Component
{
    [DataField] public float Researched = 0f;
}
