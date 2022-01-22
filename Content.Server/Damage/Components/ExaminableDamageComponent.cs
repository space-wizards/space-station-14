using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Damage.Components;

/// <summary>
///     This component shows entity damage severity
///     when it is examined by user.
/// </summary>
[RegisterComponent]
public class ExaminableDamageComponent : Component
{
    public override string Name => "ExaminableDamage";

    [DataField("maxDamage")]
    public int MaxDamage = 100;

    [DataField("messages", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<ExaminableDamagePrototype>))]
    public string MessagesProtoId = default!;

    public ExaminableDamagePrototype MessagesProto = default!;
}
