using Content.Shared.Xenoarchaeology.XenoArtifacts;

namespace Content.Server.Xenoarchaeology.XenoArtifacts;

[RegisterComponent]
public sealed class ArtifactComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public int RandomSeed;

    [ViewVariables]
    public ArtifactTree? NodeTree;

    [ViewVariables]
    public ArtifactNode? CurrentNode;

    #region Node Tree Gen
    [DataField("nodesMin")]
    public int NodesMin = 3;

    [DataField("nodesMax")]
    public int NodesMax = 9;
    #endregion

    /// <summary>
    ///     Cooldown time between artifact activations (in seconds).
    /// </summary>
    [DataField("timer")]
    [ViewVariables(VVAccess.ReadWrite)]
    public double CooldownTime = 10;

    /// <summary>
    ///     Is this artifact under some suppression device?
    ///     If true, will ignore all trigger activations attempts.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsSuppressed;

    public TimeSpan LastActivationTime;
}

[DataDefinition]
public sealed class ArtifactTree
{
    [ViewVariables]
    public ArtifactNode StartNode = default!;

    [ViewVariables]
    public readonly List<ArtifactNode> AllNodes = new();
}

[DataDefinition]
public sealed class ArtifactNode
{
    [ViewVariables]
    public string Id = string.Empty;

    [ViewVariables]
    public int Depth = 0;

    [ViewVariables]
    public readonly List<ArtifactNode> Edges = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public bool Discovered = false;

    [ViewVariables]
    public ArtifactTriggerPrototype? Trigger;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool Triggered = false;

    [ViewVariables]
    public ArtifactEffectPrototype? Effect;
}
