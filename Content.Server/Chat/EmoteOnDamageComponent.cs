using Content.Server.Chat.Systems;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Chat;

/// <summary>
/// Causes an entity to automatically emote when taking damage.
/// </summary>
[RegisterComponent, Access(typeof(EmoteOnDamageSystem)), AutoGenerateComponentPause]
public sealed partial class EmoteOnDamageComponent : Component
{
    /// <summary>
    /// Chance of preforming an emote when taking damage and not on cooldown.
    /// </summary>
    [DataField]
    public float EmoteChance = 0.5f;

    /// <summary>
    /// A set of emotes that will be randomly picked from.
    /// <see cref="EmotePrototype"/>
    /// </summary>
    [DataField]
    public HashSet<ProtoId<EmotePrototype>> Emotes = new();

    /// <summary>
    /// Also send the emote in chat.
    /// <summary>
    [DataField]
    public bool WithChat = false;

    /// <summary>
    /// Hide the chat message from the chat window, only showing the popup.
    /// This does nothing if WithChat is false.
    /// <summary>
    [DataField]
    public bool HiddenFromChatWindow = false;

    /// <summary>
    /// The simulation time of the last emote preformed due to taking damage.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan LastEmoteTime = TimeSpan.Zero;

    /// <summary>
    /// The cooldown between emotes.
    /// </summary>
    [DataField]
    public TimeSpan EmoteCooldown = TimeSpan.FromSeconds(2);
}
