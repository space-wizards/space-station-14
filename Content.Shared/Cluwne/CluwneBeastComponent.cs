using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Cluwne;

[RegisterComponent, NetworkedComponent]
public sealed class CluwneBeastComponent : Component
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
    public string? EmoteSoundsId = "CluwneBeast";

    /// <summary>
    /// Chance to turn humanoid into a cluwne with melee.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float Cluwinification = 0.15f;

    /// <summary>
    /// Amount of time cluwne beast is paralyzed for when falling over.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float ParalyzeTime = 2f;

    /// <summary>
    /// Spawner for mysterious portal.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("blueSpaceId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BlueSpaceId = "PortalGreen";

    /// <summary>
    /// Sound specifiers for honk and knock.
    /// </summary>
    [DataField("spawnsound")]
    public SoundSpecifier SpawnSound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

    [DataField("knocksound")]
    public SoundSpecifier KnockSound = new SoundPathSpecifier("/Audio/Items/airhorn.ogg");

    [DataField("cluwnesound")]
    public SoundSpecifier CluwneSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/Magic/staff_animation.ogg");

    public EmoteSoundsPrototype? EmoteSounds;

    [DataField("cluwneRoleId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public readonly string CluwneRoleId = "Cluwne Beast";

}
