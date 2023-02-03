using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Cluwne;

[RegisterComponent]
[NetworkedComponent]
public sealed class CluwneComponent : Component
{
    /// <summary>
    /// Giggle emote chances.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float GiggleChance = 0.2f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float KnockChance = 0.05f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float GiggleRandomChance = 0.08f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float GiggleCooldown = 5;

    [ViewVariables(VVAccess.ReadWrite)]
    public float RandomGiggleAttempt = 5;

    [ViewVariables(VVAccess.ReadWrite)]
    public float LastGiggleCooldown = 0f;

    /// <summary>
    /// Cluwne Emotes
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public string GiggleEmoteId = "Scream";

    [ViewVariables(VVAccess.ReadWrite)]
    public string TwitchEmoteId = "Twitch";

    [DataField("emoteId", customTypeSerializer: typeof(PrototypeIdSerializer<EmoteSoundsPrototype>))]
    public string? EmoteSoundsId = "Cluwne";

    public EmoteSoundsPrototype? EmoteSounds;

    /// <summary>
    ///Giggle emote timespan.
    /// </summary>
    [DataField("giggleGoChance", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan GiggleGoChance = TimeSpan.Zero;

    /// <summary>
    /// Amount of time cluwne is paralyzed for when falling over.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float ParalyzeTime = 2f;

    /// <summary>
    /// Sound specifiers for giggles and noises.
    /// </summary>
    [DataField("giggle")]
    public SoundSpecifier Giggle = new SoundCollectionSpecifier("CluwneScreams");

    [DataField("spawnsound")]
    public SoundSpecifier SpawnSound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

    [DataField("knocksound")]
    public SoundSpecifier KnockSound = new SoundPathSpecifier("/Audio/Items/airhorn.ogg");
}
