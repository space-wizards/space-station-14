using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.TypingIndicator;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTypingIndicatorSystem))]
public sealed partial class TypingIndicatorClothingComponent : Component
{
    /// <summary>
    ///     The indicator that the clothing wearer will use when the clothing is equipped.
    ///     If two pices of clothing both have OverrideIndicators, the most recent one will be displayed.
    /// </summary>
    [DataField]
    public ProtoId<TypingIndicatorPrototype> OverrideIndicator;
}
