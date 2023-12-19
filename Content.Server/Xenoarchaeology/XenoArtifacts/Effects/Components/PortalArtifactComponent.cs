using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
///     When activated artifact will spawn an pair portals. First - right in artifact, Second - at random point of station.
/// </summary>
[RegisterComponent, Access(typeof(PortalArtifactSystem))]
public sealed partial class PortalArtifactComponent : Component
{
    [DataField]
    public EntProtoId PortalProto = "PortalArtifact";
}
