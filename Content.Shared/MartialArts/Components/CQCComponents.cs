using Content.Shared.MartialArts;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.MartialArts;

[RegisterComponent]
public sealed partial class GrantCQCComponent : GrantMartialArtKnowledgeComponent
{

}

[RegisterComponent]
public sealed partial class CQCKnowledgeComponent : GrabStagesOverrideComponent
{
    public List<ProtoId<ComboPrototype>> RoundstartCombos = new()
    {
        "CQCSlam",
        "CQCKick",
        "CQCRestrain",
        "CQCPressure",
        "CQCConsecutive"
    };

    [DataField]
    public bool Blocked = true;
}
