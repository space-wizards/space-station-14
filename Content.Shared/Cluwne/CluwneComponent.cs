using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Cluwne;

[RegisterComponent]
[NetworkedComponent]
public sealed class CluwneComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public string GiggleEmoteId = "Scream";

    /// <summary>
    /// timings for giggles and knocks.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DamageGiggleCooldown = TimeSpan.FromSeconds(2);

    [ViewVariables(VVAccess.ReadWrite)]
    public float KnockChance = 0.05f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float GiggleRandomChance = 0.1f;

    /// <summary>
    /// The last time the zombie groaned from taking damage.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastDamageGiggle = TimeSpan.Zero;

    [DataField("emoteId", customTypeSerializer: typeof(PrototypeIdSerializer<EmoteSoundsPrototype>))]
    public string? EmoteSoundsId = "Cluwne";

    /// <summary>
    /// Amount of time cluwne is paralyzed for when falling over.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float ParalyzeTime = 2f;

    /// <summary>
    /// Sound specifiers for honk and knock.
    /// </summary>
    [DataField("spawnsound")]
    public SoundSpecifier SpawnSound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

    [DataField("knocksound")]
    public SoundSpecifier KnockSound = new SoundPathSpecifier("/Audio/Items/airhorn.ogg");
}
