using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Damage.Components;

/// <summary>
///     This component shows entity damage severity when it is examined by player.
/// </summary>
[RegisterComponent]
public sealed partial class ExaminableDamageComponent : Component
{
    [DataField("messages", required: true)]
    public ProtoId<ExaminableDamagePrototype>? MessagesProtoId;

    public ExaminableDamagePrototype? MessagesProto;
}
