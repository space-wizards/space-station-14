using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.TypingIndicator;

/// <summary>
///     Prototype to store chat typing indicator visuals.
/// </summary>
[Prototype("typingIndicator")]
public sealed partial class TypingIndicatorPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("spritePath")]
    public ResPath SpritePath = new("/Textures/Effects/speech.rsi");

    [DataField("typingState", required: true)]
    public string TypingState = default!;

    [DataField("offset")]
    public Vector2 Offset = new(0, 0);

    [DataField("shader")]
    public string Shader = "unshaded";

}
