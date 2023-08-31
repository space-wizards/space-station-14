using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.TypingIndicator;

/// <summary>
///     Store status icon prototypes ids for each possible typing state
/// </summary>
[Prototype("typingIndicator")]
public sealed class TypingIndicatorPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("typingIcon", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<StatusIconPrototype>))]
    public string TypingIcon = default!;
}
