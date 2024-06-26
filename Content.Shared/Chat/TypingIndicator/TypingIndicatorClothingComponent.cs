using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.TypingIndicator;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTypingIndicatorSystem))]
public sealed partial class TypingIndicatorClothingComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("proto", required: true)]
    public ProtoId<TypingIndicatorPrototype> TypingIndicatorPrototype = default!;

    [DataField]
    public TimeSpan? GotEquippedTime = null;
}
