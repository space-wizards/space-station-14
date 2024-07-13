using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.Components;

/// <summary>
/// Stores metadata about a particular artifact node
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedXenoArtifactSystem)), AutoGenerateComponentState]
public sealed partial class XenoArtifactNodeComponent : Component
{
    /// <summary>
    /// Depth within the graph generation.
    /// Used for sorting.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Depth;

    /// <summary>
    /// Denotes whether or not an artifact node has been activated through the required triggers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Locked = true;

    /// <summary>
    /// Strings that denote the triggers that this node has.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId TriggerTip;

    /// <summary>
    /// The entity whose graph this node is a part of.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity? Attached;

    #region Durability
    public bool Degraded => Durability <= 0;

    /// <summary>
    /// The amount of generic activations a node has left before becoming fully degraded and useless.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Durability;

    /// <summary>
    /// The maximum amount of times a node can be generically activated before becoming useless
    /// </summary>
    [DataField]
    public int MaxDurability = 5;
    #endregion

    #region Research
    [DataField, AutoNetworkedField]
    public int ResearchValue;

    [DataField, AutoNetworkedField]
    public int ConsumedResearchValue;
    #endregion
}
