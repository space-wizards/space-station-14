using Content.Shared.Destructible.Thresholds;
using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.Components;

/// <summary>
/// Component for holding artifact info that is related to amplification:
/// <para/> - budget range (min and max budget for which this node can fit
/// <para/> - list of amplification effects which are applicable to node
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedXenoArtifactSystem))]
public sealed partial class XenoArtifactNodeBudgetComponent : Component
{
    [DataField(required: true)]
    public MinMax BudgetRange;

    [DataField]
    public float PlacementInBudgetRange;

    [DataField, AutoNetworkedField]
    public XenoArtifactEffectsModifications ModifyBy = new ();
}

/// <summary>
/// Component for holding xeno artifact trigger budget info.
/// </summary>
[RegisterComponent]
public sealed partial class XenoArtifactTriggerBudgetComponent : Component
{
    [DataField(required: true)]
    public MinMax BudgetRange;

    [DataField(required: true)]
    public float ActualBudget;
}
