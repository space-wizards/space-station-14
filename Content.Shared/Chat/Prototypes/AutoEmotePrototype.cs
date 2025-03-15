using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Chat.Prototypes;

[Prototype]
public sealed partial class AutoEmotePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The ID of the emote prototype.
    /// </summary>
    [DataField("emote", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EmotePrototype>))]
    public string EmoteId = String.Empty;

    /// <summary>
    /// How often an attempt at the emote will be made.
    /// </summary>
    [DataField("interval", required: true)]
    public TimeSpan Interval;

    /// <summary>
    /// Probability of performing the emote each interval.
    /// <summary>
    [DataField("chance")]
    public float Chance = 1;

    /// <summary>
    /// Also send the emote in chat.
    /// <summary>
    [DataField("withChat")]
    public bool WithChat = true;

    /// <summary>
    /// Hide the chat message from the chat window, only showing the popup.
    /// This does nothing if WithChat is false.
    /// <summary>
    [DataField("hiddenFromChatWindow")]
    public bool HiddenFromChatWindow = false;
}
