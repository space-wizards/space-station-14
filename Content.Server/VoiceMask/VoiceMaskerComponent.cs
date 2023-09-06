using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.VoiceMask;

[RegisterComponent]
public sealed partial class VoiceMaskerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)] public string LastSetName = "Unknown";

    [DataField("actionId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionId = "ChangeVoiceMask";

    [DataField("action")] public EntityUid? Action;
}
