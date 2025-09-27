using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Cluwne;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class CluwneComponent : Component
{
    /// <summary>
    /// timings for giggles and knocks.
    /// </summary>
    [DataField]
    public TimeSpan DamageGiggleCooldown = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Amount of genetic damage dealt when they revert
    /// </summary>
    [DataField]
    public int RevertDamage = 300;

    [DataField]
    public float KnockChance = 0.05f;

    [DataField]
    public float GiggleRandomChance = 0.1f;

    /// <summary>
    /// Option to disable the random emoting. admeme usful
    /// </summary>
    [DataField]
    public bool RandomEmote = true;

    [DataField("emoteId", customTypeSerializer: typeof(PrototypeIdSerializer<EmoteSoundsPrototype>))]
    public string? EmoteSoundsId = "Cluwne";

    [DataField]
    public string? AutoEmoteId = "CluwneGiggle";

    [DataField]
    public LocId TransformMessage = "cluwne-transform";

    [DataField]
    public LocId NamePrefix = "cluwne-name-prefix";

    /// <summary>
    /// Outfit ID that the cluwne will spawn with.
    /// </summary>
    [DataField]
    public string? OutfitId = "CluwneGear";

    /// <summary>
    /// Amount of time cluwne is paralyzed for when falling over.
    /// </summary>
    [DataField]
    public float ParalyzeTime = 2f;

    /// <summary>
    /// Sound specifiers for honk and knock.
    /// </summary>
    [DataField("spawnsound")]
    public SoundSpecifier SpawnSound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

    [ViewVariables(VVAccess.ReadWrite)]
    public LocId GiggleEmote = "cluwne-giggle-emote";

    [DataField("knocksound")]
    public SoundSpecifier KnockSound = new SoundPathSpecifier("/Audio/Items/airhorn.ogg");

    [DataField]
    public LocId KnockEmote = "cluwne-knock-emote";
}
