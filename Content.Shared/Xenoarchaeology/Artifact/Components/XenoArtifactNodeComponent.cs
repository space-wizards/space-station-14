using Content.Shared.Destructible.Thresholds;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.Artifact.Components;

/// <summary>
/// Stores metadata about a particular artifact node
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedXenoArtifactSystem)), AutoGenerateComponentState, EntityCategory("XenoArtifactEffects")]
public sealed partial class XenoArtifactNodeComponent : Component
{
    /// <summary>
    /// Depth within the graph generation.
    /// Used for sorting.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Depth;

    /// <summary>
    /// Denotes whether an artifact node has been activated at least once (through the required triggers).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Locked = true;

    /// <summary>
    /// List of trigger descriptions that this node require for activation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? TriggerTip;

    /// <summary>
    /// The entity whose graph this node is a part of.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Attached;

    #region Durability
    /// <summary>
    /// Marker, is durability of node degraded or not.
    /// </summary>
    public bool Degraded => Durability <= 0;

    /// <summary>
    /// The amount of generic activations a node has left before becoming fully degraded and useless.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Durability;

    /// <summary>
    /// The maximum amount of times a node can be generically activated before becoming useless
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxDurability = 5;

    /// <summary>
    /// The variance from MaxDurability present when a node is created.
    /// </summary>
    [DataField]
    public MinMax MaxDurabilityCanDecreaseBy = new(0, 2);

    /// <summary>
    /// Minimum and maximum borders for budget.
    /// </summary>
    [DataField] // TODO: make required
    public MinMax BudgetRange;

    /// <summary>
    /// Actual budget value that this node got assigned during artifact generation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ActualBudget;

    /// <summary>
    /// Value of budget, accumulated to the moment of generating this node.
    /// Dictates amount of amplification/dampening effect will get when activated.
    /// Is used by <see cref="XenoArtifactNodeComponent.ModifyBy"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PlacementInBudget;
    #endregion

    #region Research
    /// <summary>
    /// The amount of points a node is worth with no scaling
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BasePointValue = 4000;

    /// <summary>
    /// Amount of points available currently for extracting.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ResearchValue;

    /// <summary>
    /// Amount of points already extracted from node.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ConsumedResearchValue;
    #endregion
}
