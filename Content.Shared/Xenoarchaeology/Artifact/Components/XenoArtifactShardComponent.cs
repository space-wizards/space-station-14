using Content.Shared.NameIdentifier;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;


namespace Content.Shared.Xenoarchaeology.Artifact.Components;

/// <summary>
/// Xenoartifact Shard component for handling shard nodes
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoArtifactShardComponent : Component
{
    /// <summary>
    /// The group prototype of the identifier.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<NameIdentifierGroupPrototype>? Group = "XenoArtifactShard";
}
