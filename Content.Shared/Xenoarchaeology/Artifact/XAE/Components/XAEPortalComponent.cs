using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
///     When activated artifact will spawn a pair of portals. First - right in artifact, Second - at random point of station.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XAEPortalComponent : Component
{
    /// <summary>
    /// Entity that should be spawned as portal.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId PortalProto = "PortalArtifact";
}
