using Content.Shared.MartialArts;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.MartialArts;

public abstract partial class GrantMartialArtKnowledgeComponent : Component
{
    [DataField]
    public bool Used = false;
}
