using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// Artifact that ignites surrounding entities when triggered.
/// </summary>
[RegisterComponent]
public sealed partial class PolyArtifactComponent : Component
{
    [DataField("range")]
    public float Range = 3f;

    [ViewVariables(VVAccess.ReadWrite), DataField("polyEntity", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PolyEntity = "ArtifactMonkey";
}
