using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.TypingIndicator;

/// <summary>
///     Prototype to store chat typing indicator visuals.
/// </summary>
[Prototype("typingIndicator")]
public sealed class TypingIndicatorPrototype : IPrototype
{
    [ViewVariables]
    [IdDataFieldAttribute]
    public string ID { get; } = default!;

    [ViewVariables]
    [DataField("icon", required: true)]
    public SpriteSpecifier Icon = SpriteSpecifier.Invalid;

}
