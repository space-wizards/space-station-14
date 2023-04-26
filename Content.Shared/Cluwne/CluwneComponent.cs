using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Roles;

namespace Content.Shared.Cluwne;

[RegisterComponent]
[NetworkedComponent]
public sealed class CluwneComponent : Component
{
    /// <summary>
    /// timings for giggles and knocks.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DamageGiggleCooldown = TimeSpan.FromSeconds(2);

    [ViewVariables(VVAccess.ReadWrite)]
    public float KnockChance = 0.05f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float GiggleRandomChance = 0.1f;

    [DataField("emoteId", customTypeSerializer: typeof(PrototypeIdSerializer<EmoteSoundsPrototype>))]
    public string? EmoteSoundsId = "Cluwne";

    [ViewVariables(VVAccess.ReadWrite)]
    public float Cluwinification = 0.15f;

    [DataField("cluwneRoleId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public readonly string CluwneRoleId = "Cluwne Beast";

    [DataField("isCluwne")]
    public bool IsCluwne = true;

    [DataField("autoEmoteSound")]
    public readonly string AutoEmoteSound = "CluwneGiggle";

    [DataField("cluwneOnMelee")]
    public bool CluwneOnMelee = false;

    [DataField("isBeast")]
    public bool IsBeast = false;

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

    [DataField("cluwnesound")]
    public SoundSpecifier CluwneSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/Magic/staff_animation.ogg");
}
