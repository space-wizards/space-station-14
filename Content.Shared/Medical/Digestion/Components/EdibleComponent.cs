using Content.Shared.FixedPoint;
using Content.Shared.Medical.Digestion.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Digestion.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EdibleComponent : Component
{
    [DataField(required:true)]
    public ProtoId<DigestionTypePrototype> DigestionType;

    [DataField]
    public bool EatWhole = false;

    /// <summary>
    /// How many ReagentUnits per second should be transferred to the dissolving solution
    /// </summary>
    [DataField]
    public float DigestionRate = 1;

    [DataField]
    public string DigestionSolutionId = "food";

    [DataField, AutoNetworkedField]
    public EntityUid? CachedDigestionSolution = null;

}
