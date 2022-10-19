using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.TypingIndicator;

/// <summary>
///     Prototype to store chat typing indicator visuals.
/// </summary>
[Prototype("typingIndicator")]
public readonly record struct TypingIndicatorPrototype : IPrototype
{
    [IdDataFieldAttribute]
    public string ID { get; } = default!;

    [DataField("spritePath")] public readonly ResourcePath SpritePath = new("/Textures/Effects/speech.rsi");

    [DataField("typingState", required: true)]
    public readonly string TypingState = default!;

    [DataField("offset")] public readonly Vector2 Offset = new(0.5f, 0.5f);

    [DataField("shader")] public readonly string Shader = "unshaded";

}
