using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Damage.Components;

/// <summary>
///     This component shows entity damage severity when it is examined by player.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ExaminableDamageComponent : Component
{
    [DataField("messages", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<ExaminableDamagePrototype>))]
    public ProtoId<ExaminableDamagePrototype>? MessagesProtoId;
}
