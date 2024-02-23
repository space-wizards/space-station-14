using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Silicons.Borgs.Components;

[RegisterComponent]
public sealed partial class BorgCrewManifestViewerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite),
     DataField("actionViewCrewManifest", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionViewCrewManifest = "ActionViewCrewManifest";
}
