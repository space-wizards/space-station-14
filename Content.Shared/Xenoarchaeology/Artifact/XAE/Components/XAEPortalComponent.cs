using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
///     When activated artifact will spawn an pair portals. First - right in artifact, Second - at random point of station.
/// </summary>
[RegisterComponent, Access(typeof(XAEPortalSystem))]
public sealed partial class XAEPortalComponent : Component
{
    [DataField]
    public EntProtoId PortalProto = "PortalArtifact";
}
