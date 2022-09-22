using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.VoiceMask;

[RegisterComponent]
public sealed class VoiceMaskerComponent : Component
{
    public string LastSetName = "Unknown";

    [DataField("action", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string Action = "ChangeVoiceMask";
}
